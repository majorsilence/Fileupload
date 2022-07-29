using Renci.SshNet;

namespace FileUpload.Providers;

/// <summary>
///     SSH file transfer protocol
/// </summary>
public class Sftp : IFileProvider
{
    private readonly string host;
    private readonly string password;
    private readonly int port;
    private readonly string username;
    private SftpClient client;
    private bool disposed;
    private bool isLoggedIn;

    public Sftp(string host, int port, string username, string password)
    {
        this.host = host;
        this.port = port;
        this.username = username;
        this.password = password;
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
        Login();
        client.DownloadFile(path, output);
    }

    private void UploadFile(string path, Stream input)
    {
        Login();
        client.UploadFile(input, path);
    }

    private void Login()
    {
        if (isLoggedIn) return;

        client = new SftpClient(host, port, username, password);
        client.Connect();

        isLoggedIn = true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
            if (client != null)
            {
                if (client.IsConnected) client.Disconnect();
                client.Dispose();
            }

        disposed = true;
    }
}