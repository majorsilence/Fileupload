namespace FileUpload.Providers;

public interface IFileProvider : IDisposable
{
    Task UploadFileAsync(string path, Stream input);
    Task DownloadFileAsync(string path, Stream output);
}