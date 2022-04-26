using FHSCAzureFunction.AppConfig;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FHSCAzureFunction
{
    public class BlobStorageService
    {
        string AccessKey { get; set; }
        string ContainerName { get; set; }

        public BlobStorageService()
        {
            this.AccessKey = AppConfiguration.GetConfiguration("StorageAccount");
            this.ContainerName = AppConfiguration.GetContainer("Container");
        }

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

        public async void DeleteBlobData(string fileUrl)
        {
            Uri uriObj = new Uri(fileUrl);
            string BlobName = Path.GetFileName(uriObj.LocalPath);

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(AccessKey);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = "uploads";
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);

            string pathPrefix = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd") + "/";
            CloudBlobDirectory blobDirectory = cloudBlobContainer.GetDirectoryReference(pathPrefix);
            // get block blob reference    
            CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(BlobName);

            // delete blob from container        
            await blockBlob.DeleteAsync();
        }


        private string GenerateFileName(string fileName)
        {
            string strFileName = string.Empty;
            string[] strName = fileName.Split('.');
            strFileName = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd") + "/" + DateTime.Now.ToUniversalTime().ToString("yyyyMMdd\\THHmmssfff") + "." + strName[strName.Length - 1];
            return strFileName;
        }

        private async Task<string> UploadFileToBlobAsync(string strFileName, MemoryStream fileData, string fileMimeType)
        {
            try
            {
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(AccessKey);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                //string strContainerName = "uploads";
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                //string fileName = this.GenerateFileName(strFileName);

                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                }

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
            Uri uriObj = new Uri(fileUrl);
            try
            {
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(AccessKey);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                //string strContainerName = "uploads";
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                CloudBlockBlob blockBlob = new CloudBlockBlob(uriObj);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memoryStream);
                    return memoryStream;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
