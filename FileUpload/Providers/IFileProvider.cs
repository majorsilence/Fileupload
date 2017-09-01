using System;
using System.Threading.Tasks;

namespace FileUpload.Providers
{
    public interface IFileProvider : IDisposable
    {
        void UploadFile(string path, System.IO.Stream input);
        void DownloadFile(string path, System.IO.Stream output);
        Task UploadFileAsync(string path, System.IO.Stream input);
        Task DownloadFileAsync(string path, System.IO.Stream output);
    }
}