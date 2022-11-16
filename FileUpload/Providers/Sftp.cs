using Renci.SshNet;

namespace FileUpload.Providers;

/// <summary>
/// SSH file transfer protocol
/// </summary>
public class Sftp : IFileProvider
{
    private SftpClient client;
    private bool disposed;

    public Sftp(string host, int port, string username, string password)
    {
        client = new SftpClient(host, port, username, password);
        client.ConnectionInfo.RetryAttempts = 3;
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(60);
    }

    public Task DownloadFileAsync(string path, Stream output)
    {
        // fixme:  Use async ssh implemenation
        return Task.Run(() => { DownloadFile(path, output); });
    }

    public Task UploadFileAsync(string path, Stream input)
    {
        // fixme:  Use async ssh implemenation
        return Task.Run(() => { UploadFile(path, input); });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void DownloadFile(string path, Stream output)
    {
        if (!client.IsConnected)
        {
            client.Connect();
        }

        client.DownloadFile(path, output);
    }

    private void UploadFile(string path, Stream input)
    {
        if (!client.IsConnected)
        {
            client.Connect();
        }

        client.UploadFile(input, path);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            if (client.IsConnected) client.Disconnect();
            client.Dispose();
        }

        disposed = true;
    }
}