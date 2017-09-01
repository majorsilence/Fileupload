using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUpload
{
    class Program
    {
        static void Main(string[] args)
        {

            using (var provider = GetProvider())
            {
                // Do stuff here
                // provider.DownloadFile(...);
                // provider.UploadFile(...);

            }
        }

        private static Providers.IFileProvider GetProvider()
        {
            string provider = ConfigurationManager.AppSettings["FileProvider"];
            switch (provider)
            {
                case "sftp":
                    string host = ConfigurationManager.AppSettings["Host"];
                    int port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]);
                    string username = ConfigurationManager.AppSettings["Username"];
                    string password = ConfigurationManager.AppSettings["Password"];
                    return new Providers.Sftp(host, port, username, password);
                case "azureblob":
                    string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
                    string container = ConfigurationManager.AppSettings["Container"];
                    return new Providers.AzureBlob(connectionString, container);
                default:
                    return null;
            }
        }
    }
}
