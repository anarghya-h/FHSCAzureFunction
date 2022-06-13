using FHSCAzureFunction.AppConfig;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Services
{
    public class BlobStorageService
    {
        #region Private member variables
        string AccessKey { get; set; }
        string ContainerName { get; set; }
        #endregion

        #region Constructors
        public BlobStorageService()
        {
            this.AccessKey = AppConfiguration.GetConfiguration("StorageAccount");
            this.ContainerName = AppConfiguration.GetContainer("Container");
        }
        #endregion

        #region Public Methods
        //Method to upload the file data in bytes in the storage account container
        public string UploadFileToBlob(string strFileName, MemoryStream fileData, string fileMimeType)
        {
            try
            {

                var _task = Task.Run(() => this.UploadFileToBlobAsync(strFileName, fileData, fileMimeType));
                _task.Wait();
                string fileUrl = _task.Result;
                return fileUrl;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        //Method to download the file from the Storage account onto a memory stream
        public MemoryStream DownloadFileFromBlob(string fileUrl)
        {
            try
            {
                var _task = Task.Run(() => this.DownloadFileFromBlobAsync(fileUrl));
                _task.Wait();
                MemoryStream fileStream = _task.Result;
                return fileStream;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
        #endregion

        #region Private methods
        private async Task<string> UploadFileToBlobAsync(string strFileName, MemoryStream fileData, string fileMimeType)
        {
            try
            {
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(AccessKey);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                
                await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                if (strFileName != null && fileData != null)
                {
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(strFileName);
                    cloudBlockBlob.Properties.ContentType = fileMimeType;
                    await cloudBlockBlob.UploadFromStreamAsync(fileData);
                    return cloudBlockBlob.Uri.AbsoluteUri;
                }
                return "";
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private async Task<MemoryStream> DownloadFileFromBlobAsync(string fileUrl)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(AccessKey);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                
                CloudBlockBlob blockBlob = new CloudBlockBlob(new Uri(fileUrl));
                              
                await blockBlob.DownloadToStreamAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
                
            }
            catch (Exception e)
            {
                memoryStream.Dispose();
                throw e;
            }
        }
        #endregion
    }
}
