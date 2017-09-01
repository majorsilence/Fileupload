using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types
using Microsoft.Azure; // Namespace for CloudConfigurationManager

namespace FileUpload.Providers
{
    public class AzureBlob : IFileProvider
    {
        readonly string storageConnectionString;
        readonly string containerName;

        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;

        bool isLoggedIn = false;

        bool disposed = false;

        public AzureBlob(string storageConnectionString, string containerName)
        {
            this.storageConnectionString = storageConnectionString;
            this.containerName = containerName;
        }

        private void Login()
        {
            if (isLoggedIn)
            {
                return;
            }

            // Retrieve storage account from connection string.
            storageAccount = CloudStorageAccount.Parse(
   CloudConfigurationManager.GetSetting(storageConnectionString));

            // Create the blob client.
            blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            container = blobClient.GetContainerReference(containerName);

            isLoggedIn = true;
        }

        public void DownloadFile(string blobName, System.IO.Stream output)
        {
            Login();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the "blobName" blob with contents from a local file.
            blockBlob.DownloadToStream(output);
        }

        public Task DownloadFileAsync(string blobName, System.IO.Stream output)
        {
            Login();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the "blobName" blob with contents from a local file.
            return blockBlob.DownloadToStreamAsync(output);
        }

        public void UploadFile(string blobName, System.IO.Stream input)
        {
            Login();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the "blobName" blob with contents from a local file.
            blockBlob.UploadFromStream(input);
        }

        public Task UploadFileAsync(string blobName, System.IO.Stream input)
        {
            Login();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the "blobName" blob with contents from a local file.
            return blockBlob.UploadFromStreamAsync(input);

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

            }

            disposed = true;
        }

    }
}
