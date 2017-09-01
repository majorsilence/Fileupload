using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUpload.Providers
{
    public class Sftp : IFileProvider
    {
        readonly int port;
        readonly string username;
        readonly string password;
        readonly string host;
        bool isLoggedIn = false;
        Renci.SshNet.SftpClient client;
        bool disposed = false;

        public Sftp(string host, int port, string username, string password)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
        }

        private void Login()
        {
            if (isLoggedIn)
            {
                return;
            }

            client = new Renci.SshNet.SftpClient(host, port, username, password);
            client.Connect();

            isLoggedIn = true;
        }

        public void DownloadFile(string path, System.IO.Stream output)
        {
            Login();
            client.DownloadFile(path, output);
        }

        public Task DownloadFileAsync(string path, System.IO.Stream output)
        {
            // fixme:  Use async ssh implemenation
            return Task.Run(() =>
            {
                DownloadFile(path, output);
            });
        }

        public void UploadFile(string path, System.IO.Stream input)
        {
            Login();
            client.UploadFile(input, path);
        }

        public Task UploadFileAsync(string path, System.IO.Stream input)
        {
            // fixme:  Use async ssh implemenation
            return Task.Run(() =>
            {
                UploadFile(path, input);
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (client != null)
                {
                    if (client.IsConnected)
                    {
                        client.Disconnect();
                    }
                    client.Dispose();
                }
            }

            disposed = true;
        }

    }
}
