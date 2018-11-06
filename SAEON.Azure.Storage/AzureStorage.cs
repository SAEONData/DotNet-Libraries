using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SAEON.Azure.Storage
{
    public abstract class AzureTable : TableEntity
    {
        public virtual void SetKeys()
        {
            throw new NotImplementedException();
        }
    }

    public class AzureStorage
    {
        private CloudStorageAccount storageAccount = null;
        private CloudBlobClient blobClient = null;
        private CloudQueueClient queueClient = null;
        private CloudTableClient tableClient = null;

        public AzureStorage(string connectionString)
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            queueClient = storageAccount.CreateCloudQueueClient();
            tableClient = storageAccount.CreateCloudTableClient();
        }

        ~AzureStorage()
        {
            blobClient = null;
            queueClient = null;
            tableClient = null;
            storageAccount = null;
        }

        #region Containers

        public async Task<bool> DeleteContainerAsync(string name)
        {
            CloudBlobContainer container = GetContainer(name);
            return await container.DeleteIfExistsAsync();
        }

        public async Task<CloudBlobContainer> EnsureContainerAsync(string name)
        {
            CloudBlobContainer container = GetContainer(name);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public CloudBlobContainer GetContainer(string name)
        {
            return blobClient.GetContainerReference(name.ToLower());
        }

        #endregion Containers

        #region Blobs

        public async Task DeleteBlobAsync(CloudBlobContainer container, string name)
        {
            await container.DeleteBlobAsync(name);
        }

        public static async Task<bool> DownloadBlobAsync(CloudBlobContainer container, string name, Stream stream)
        {
            return await container.DownloadBlobAsync(name, stream);
        }

        public static async Task<List<string>> FolderList(CloudBlobContainer container, string folder)
        {
            return await container.FolderList(folder);
        }

        public static async Task UploadBlobAsync(CloudBlobContainer container, string name, Stream stream)
        {
            await container.UploadBlobAsync(name, stream);
        }

        public static async Task UploadBlobAsync(CloudBlobContainer container, string name, byte[] byteArray)
        {
            await container.UploadBlobAsync(name, byteArray);
        }

        public static async Task UploadBlobAsync(CloudBlobContainer container, string name, string content)
        {
            await container.UploadBlobAsync(name, content);
        }

        public static async Task UploadBlobIfNotExistsAsync(CloudBlobContainer container, string name, Stream stream)
        {
            await container.UploadBlobIfNotExistsAsync(name, stream);
        }

        public static async Task UploadBlobIfNotExistsAsync(CloudBlobContainer container, string name, byte[] byteArray)
        {
            await container.UploadBlobIfNotExistsAsync(name, byteArray);
        }

        public static async Task UploadBlobIfNotExistsAsync(CloudBlobContainer container, string name, string content)
        {
            await container.UploadBlobIfNotExistsAsync(name, content);
        }

        #endregion Blobs

        #region

        public async Task DeleteQueueAsync(string name)
        {
            CloudQueue queue = GetQueue(name);
            await queue.DeleteIfExistsAsync();
        }

        public async Task<CloudQueue> EnsureQueueAsync(string name)
        {
            CloudQueue queue = GetQueue(name);
            await queue.CreateIfNotExistsAsync();
            return queue;
        }

        public CloudQueue GetQueue(string name)
        {
            CloudQueue queue = queueClient.GetQueueReference(name.ToLower());
            return queue;
        }

        #endregion

        #region Tables

        public async Task DeleteTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            await table.DeleteIfExistsAsync();
        }

        public async Task<CloudTable> EnsureTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public CloudTable GetTable(string name)
        {
            CloudTable table = tableClient.GetTableReference(name);
            return table;
        }

        #endregion

        #region AzureTables
        #endregion
    }

    public static class AzureStorageExtensions
    {
        #region Blobs

        public static async Task DeleteBlobAsync(this CloudBlobContainer container, string name)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await blockBlob.DeleteIfExistsAsync();
        }

        public static async Task<bool> DownloadBlobAsync(this CloudBlobContainer container, string name, Stream stream)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
            {
                return false;
            }
            await blockBlob.DownloadToStreamAsync(stream);
            return true;
        }

        public static async Task<List<string>> FolderList(this CloudBlobContainer container, string folder)
        {
            string GetFileNameFromBlobURI(Uri theUri, string containerName)
            {
                string theFile = theUri.ToString();
                int dirIndex = theFile.IndexOf(containerName);
                string oneFile = theFile.Substring(dirIndex + containerName.Length + 1,
                    theFile.Length - (dirIndex + containerName.Length + 1));
                return oneFile;
            }

            var result = new List<string>();
            BlobContinuationToken token = null;
            do
            {
                var dir = container.GetDirectoryReference(folder);
                var results = await dir.ListBlobsSegmentedAsync(false, BlobListingDetails.None, null, token, null, null);
                foreach (var blob in results.Results)
                {
                    if (blob is CloudBlockBlob)
                    {
                        result.Add(GetFileNameFromBlobURI(blob.Uri, container.Name));
                    }
                }
                token = results.ContinuationToken;
            }
            while (token != null);
            return result;
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, Stream stream)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await blockBlob.DeleteIfExistsAsync();
            await blockBlob.UploadFromStreamAsync(stream);
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, byte[] byteArray)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await blockBlob.DeleteIfExistsAsync();
            await blockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, string content)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await blockBlob.DeleteIfExistsAsync();
            await blockBlob.UploadTextAsync(content);
        }

        public static async Task UploadBlobIfNotExistsAsync(this CloudBlobContainer container, string name, Stream stream)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadFromStreamAsync(stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this CloudBlobContainer container, string name, byte[] byteArray)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this CloudBlobContainer container, string name, string content)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadTextAsync(content);
            }
        }

        #endregion

        #region Queues
        #endregion

        #region Tables
        #endregion

        #region AzureTables

        public static void CopyFrom(this AzureTable destination, AzureTable source)
        {
            var fields = destination.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (!field.Name.Equals("PartitionKey", StringComparison.CurrentCultureIgnoreCase) &&
                    !field.Name.Equals("RowKey", StringComparison.CurrentCultureIgnoreCase) &&
                    !field.Name.Equals("Timestamp", StringComparison.CurrentCultureIgnoreCase) &&
                    !field.Name.Equals("ETag", StringComparison.CurrentCultureIgnoreCase))
                {
                    field.SetValue(destination, field.GetValue(source));
                }
            }
        }

        public static async Task<T> DeleteEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("DeleteEntity: Null entity");
            }

            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("DeleteEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            return (T)(await table.ExecuteAsync(TableOperation.Delete(oldEntity))).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey))).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : AzureTable
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey))).Result;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, TableQuery<T> query) where T : AzureTable, new()
        {
            var result = new List<T>();
            TableContinuationToken token = null;
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token);
                result.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return result;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table) where T : AzureTable, new()
        {
            return await GetEntitiesAsync(table, new TableQuery<T>());
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, string partitionKey) where T : AzureTable, new()
        {
            var query = new TableQuery<T>();
            query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            return await GetEntitiesAsync(table, query);
        }

        public static async Task<T> InsertEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Insert(entity))).Result;
        }

        public static async Task<T> InsertOrMergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertOrMergeEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrMerge(entity))).Result;
        }

        public static async Task<T> InsertOrReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertOrReplaceEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrReplace(entity))).Result;
        }

        public static async Task<T> MergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("MergeEntity: Null entity");
            }

            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("MergeEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            oldEntity.CopyFrom(entity);
            oldEntity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Merge(oldEntity))).Result;
        }

        public static async Task<T> ReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTable
        {
            if (entity == null)
            {
                throw new NullReferenceException("ReplaceEntity: Null entity");
            }

            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("ReplaceEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            oldEntity.CopyFrom(entity);
            oldEntity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Replace(oldEntity))).Result;
        }

        #endregion
    }
}