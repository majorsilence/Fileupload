using Azure.Storage.Blobs;

// https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet?tabs=environment-variable-windows#create-a-container


namespace FileUpload.Providers;

public class AzureBlob : IFileProvider
{
    private readonly string containerName;
    private readonly string storageConnectionString;
    private BlobServiceClient blobClient;
    private BlobContainerClient container;

    private bool disposed;

    private bool isLoggedIn;

    public AzureBlob(string storageConnectionString, string containerName)
    {
        this.storageConnectionString = storageConnectionString;
        this.containerName = containerName;
    }

    public async Task DownloadFileAsync(string blobName, Stream output)
    {
        await LoginAsync();

        var blockBlob = container.GetBlobClient(blobName);

        // Create or overwrite the "blobName" blob with contents from a local file.
        await blockBlob.DownloadToAsync(output);
    }

    public async Task UploadFileAsync(string blobName, Stream input)
    {
        await LoginAsync();

        var client = container.GetBlobClient(blobName);

        // Create or overwrite the "blobName" blob with contents from a local file.
        await client.UploadAsync(input);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task LoginAsync()
    {
        if (isLoggedIn) return;

        // Create the blob client.
        blobClient = new BlobServiceClient(storageConnectionString);

        // Retrieve reference to a previously created container.
        container = await blobClient.CreateBlobContainerAsync(containerName);

        isLoggedIn = true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
        }

        disposed = true;
    }
}