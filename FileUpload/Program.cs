using FileUpload.Providers;

namespace FileUpload;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var sourcePath = string.Empty;
        var destPath = string.Empty;
        var download = false;
        using (var provider = GetProvider(args, out sourcePath, out destPath, out download))
        {
            // Do stuff here
            if (download)
            {
                await using var fs = new FileStream(destPath, FileMode.CreateNew);
                await provider.DownloadFileAsync(sourcePath, fs);
            }
            else
            {
                await using var fs = new FileStream(sourcePath, FileMode.Open);
                await provider.UploadFileAsync(destPath, fs);
            }
        }
    }

    private static IFileProvider GetProvider(string[] args, out string sourcePath, out string destPath,
        out bool download)
    {
        var provider = string.Empty;
        sourcePath = string.Empty;
        destPath = string.Empty;
        download = false;
        // SFTP
        var sftpUsername = string.Empty;
        var sftpPassword = string.Empty;
        var sftpHost = string.Empty;
        var sftpPort = string.Empty;
        // AZURE
        var azureConnectionString = string.Empty;
        var azureContainer = string.Empty;
        // BOX
        var boxJsonConfigPath = string.Empty;
        var boxJsonConfigAsString = string.Empty;
        bool boxPermitFileModification = false;

        if (args.Length <= 2)
        {
            PrintHelp();
            Environment.Exit(0);
        }

        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--provider")
            {
                provider = args[i + 1];
            }
            else if (args[i] == "--sourcepath")
            {
                sourcePath = args[i + 1];
            }
            else if (args[i] == "--destpath")
            {
                destPath = args[i + 1];
            }
            else if (args[i] == "--sftpusername")
            {
                sftpUsername = args[i + 1];
            }
            else if (args[i] == "--sftppassword")
            {
                sftpPassword = args[i + 1];
            }
            else if (args[i] == "--sftphost")
            {
                sftpHost = args[i + 1];
            }
            else if (args[i] == "--sftpport")
            {
                sftpPort = args[i + 1];
            }
            else if (args[i] == "--azureconnectionstring")
            {
                azureConnectionString = args[i + 1];
            }
            else if (args[i] == "--azurecontainer")
            {
                azureContainer = args[i + 1];
            }
            else if (args[i] == "--boxjsonconfigpath")
            {
                boxJsonConfigPath = args[i + 1];
            }
            else if (args[i] == "--boxjsonconfigstring")
            {
                boxJsonConfigAsString = args[i + 1];
            }
            else if (args[i] == "--download")
            {
                download = true;
            }
            else if (args[i] == "--box-permit-file-update")
            {
                boxPermitFileModification = true;
            }
            else if (args[i] == "/?" || args[i] == "-help" || args[i] == "help" || args[i] == "--help")
            {
                PrintHelp();
                Environment.Exit(0);
            }


        switch (provider)
        {
            case "sftp":
                var host = sftpHost;
                var port = Convert.ToInt32(sftpPort);
                var username = sftpUsername;
                var password = sftpPassword;
                return new Sftp(host, port, username, password);
            case "azureblob":
                var connectionString = azureConnectionString;
                var container = azureContainer;
                return new AzureBlob(connectionString, container);
            case "box":
                if (!string.IsNullOrWhiteSpace(boxJsonConfigPath))
                    boxJsonConfigAsString = File.ReadAllText(boxJsonConfigPath);

                return new Providers.Box(boxJsonConfigAsString, boxPermitFileModification);
            default:
                return null;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Select a provider");
        Console.WriteLine(
            "  --provider <the type> --sourcepath \"/path/to/local/file/to/upload/filename.ext\" --destpath \"/path/on/destination/server/filename.ext\"");
        Console.WriteLine("  Available types:  sftp, azureblob, box");
        Console.WriteLine("  This tool defaults to uploading files.");
        Console.WriteLine("  To switch to downloads set --download");
        Console.WriteLine("");
        Console.WriteLine("sftp options");
        Console.WriteLine("  --sftpusername \"<username>\"");
        Console.WriteLine("  --sftppassword \"<password>\"");
        Console.WriteLine("  --sftphost \"<host>\"");
        Console.WriteLine("  --sftpport <port>");
        Console.WriteLine("");
        Console.WriteLine("azureblob options");
        Console.WriteLine("  --azureconnectionstring \"<connectionstring>\"");
        Console.WriteLine("  --azurecontainer \"<container>\"");
        Console.WriteLine("");
        Console.WriteLine("box options");
        Console.WriteLine("  --boxjsonconfigpath \"<filepath to json config>\"");
        Console.WriteLine("  --boxjsonconfigstring \"<ready to use json string instead of filepath>\"");
        Console.WriteLine("  --box-permit-file-update");
        Console.WriteLine("      If a file exists with the same name in the destination folder then it will be overwritten");
        Console.WriteLine("");
        Console.WriteLine("Examples");
        Console.WriteLine("");
        Console.WriteLine(
            "./FileUploader.exe --provider box --boxjsonconfigpath \"/some/local/path/boxconfig.json\"");
    }
}