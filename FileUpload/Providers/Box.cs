using Box.V2;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;

namespace FileUpload.Providers;

/// <summary>
///     box.com jwt auth.
///     Go to box.com, add the user as a member of the base folder that it should have access.
///     find the users email with the box.com cli tool with the following command.
///     box users:get
/// </summary>
public class Box : IFileProvider
{
    private static string adminToken;
    private readonly string _jsonConfig;
    private readonly bool _boxPermitFileModification;

    public Box(string jsonConfig, bool boxPermitFileModification)
    {
        _jsonConfig = jsonConfig;
        _boxPermitFileModification = boxPermitFileModification;
    }

    private BoxClient client { get; set; }


    public async Task UploadFileAsync(string path, Stream input)
    {
        await LoginAsync();

        var split = path.Split("/");
        var name = split.LastOrDefault();
        var folders = split.Where(p => !string.Equals(p, name) && !string.IsNullOrWhiteSpace(p)).ToArray();

        // 0 starts from the base folder that the credentials have access
        var parentId = "0";
        foreach (var folder in folders)
        {
            string folderId = await FindFolderId(parentId, folder);
            if (string.IsNullOrWhiteSpace(folderId))
            {
                
                var folderParams = new BoxFolderRequest()
                {
                    Name = folder,
                    Parent = new BoxRequestEntity()
                    {
                        Id = parentId
                    }
                };
                await client.FoldersManager.CreateAsync(folderParams);
            }
            else
            {
                parentId = folderId;
            }
        }

        try
        {
            await client.FilesManager.UploadAsync(new BoxFileRequest()
                {
                    Name = name,
                    Parent = new BoxFolderRequest()
                    {
                        Id = parentId
                    }
                },
                input);
            return;
        }
        catch (BoxAPIException)
        {
            if (!_boxPermitFileModification)
            {
                throw;
            }
        }
        // overwrite existing file
        string fileId = await FindFileId(parentId, name, 0);
        await client.FilesManager.UploadNewVersionAsync(name, fileId, input);
    }

    public async Task DownloadFileAsync(string path, Stream output)
    {
        await LoginAsync();

        var split = path.Split("/");
        var name = split.LastOrDefault();
        var folders = split.Where(p => !string.Equals(p, name) && !string.IsNullOrWhiteSpace(p)).ToArray();

        // 0 starts from the base folder that the credentials have access
        var folderId = "0";
        foreach (var folder in folders)
        {
            folderId = await FindFolderId(folderId, folder);
            if (string.IsNullOrWhiteSpace(folderId))
            {
                throw new Exception("Folder not found");
            }
        }

        var folderManager = await client.FoldersManager.GetFolderItemsAsync(folderId, 5000);
        var fileEntry =
            folderManager.Entries.FirstOrDefault(
                p => string.Equals(p.Name, name, StringComparison.InvariantCultureIgnoreCase));

        if (fileEntry == null)
        {
            throw new Exception("File not found");
        }

        BoxFile f = await client.FilesManager.GetInformationAsync(fileEntry.Id);
        var fStream = await client.FilesManager.DownloadAsync(fileEntry.Id);

        await fStream.CopyToAsync(output);
    }

    public void Dispose()
    {
    }

    private async Task<string> FindFolderId(string folderId, string findSubFolderNamed)
    {
        var results = await client.FoldersManager.GetFolderItemsAsync(folderId, 100);
        var foundId = results.Entries.FirstOrDefault(p =>
            string.Equals(p.Name, findSubFolderNamed, StringComparison.InvariantCultureIgnoreCase))?.Id;
        return foundId;
    }

    private async Task<string> FindFileId(string folderId, string fileName, int page)
    {
        if (page > 10)
        {
            throw new Exception("File not found");
        }

        var results = await client.FoldersManager.GetFolderItemsAsync(folderId, 500, page * 500);
        var found = results.Entries.FirstOrDefault(p =>
            string.Equals(p.Name, fileName, StringComparison.InvariantCultureIgnoreCase));
        if (found == null)
        {
            return await FindFileId(folderId, fileName, page + 1);
        }

        return found.Id;
    }

    private async Task LoginAsync()
    {
        // Login to developer site and create app at https://developer.box.com/
        // brings you to https://app.box.com/developers/console

        // Enterprise way - https://github.com/box/box-windows-sdk-v2
        // Setup document https://developer.box.com/docs/setting-up-a-jwt-app

        // Configure
        var config = BoxConfig.CreateFromJsonString(_jsonConfig);
        var boxJWT = new BoxJWTAuth(config);

        // Authenticate
        await AdminTokenAsync(boxJWT);

        try
        {
            client = boxJWT.AdminClient(adminToken);
        }
        catch (Exception)
        {
            // update token
            await AdminTokenAsync(boxJWT, true);
            client = boxJWT.AdminClient(adminToken);
        }
    }

    private static async Task AdminTokenAsync(BoxJWTAuth boxJWT, bool reset = false)
    {
        if (reset) adminToken = string.Empty;

        if (string.IsNullOrWhiteSpace(adminToken))
            //valid for 60 minutes so should be cached and re-used
            adminToken = await boxJWT.AdminTokenAsync();
    }
}