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
    public abstract class AzureTableEntity : TableEntity
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
        public static bool UseExists { get; set; } = true;

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
            if (!UseExists)
            {
                return await container.DeleteIfExistsAsync();
            }
            else if (await container.ExistsAsync())
            {
                await container.DeleteAsync();
                return true;
            }
            return false;
        }

        public async Task<CloudBlobContainer> EnsureContainerAsync(string name)
        {
            CloudBlobContainer container = GetContainer(name);
            if (!UseExists)
            {
                await container.CreateIfNotExistsAsync();
            }
            else if (!await container.ExistsAsync())
            {
                await container.CreateAsync();
            }
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

        #region Queues

        public async Task DeleteQueueAsync(string name)
        {
            CloudQueue queue = GetQueue(name);
            if (!UseExists)
            {
                await queue.DeleteIfExistsAsync();
            }
            else if (await queue.ExistsAsync())
            {
                await queue.DeleteAsync();
            }
        }

        public async Task<CloudQueue> EnsureQueueAsync(string name)
        {
            CloudQueue queue = GetQueue(name);
            if (!UseExists)
            {
                await queue.CreateIfNotExistsAsync();
            }
            else if (! await queue.ExistsAsync())
            {
                await queue.CreateAsync();
            }
            return queue;
        }

        public CloudQueue GetQueue(string name)
        {
            CloudQueue queue = queueClient.GetQueueReference(name.ToLower());
            return queue;
        }

        public async Task<List<string>> ListQueuesAsync()
        {
            var result = new List<string>();
            QueueContinuationToken continuationToken = null;
            var allTables = new List<CloudTable>();
            do
            {
                var segment = await queueClient.ListQueuesSegmentedAsync(continuationToken);
                foreach (var queue in segment.Results)
                {
                    result.Add(queue.Name);
                }
                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);
            return result;
        }

#if NET461 || NET472
        public List<string> ListQueues()
        {
            var result = new List<string>();
            foreach (var queue in queueClient.ListQueues())
            {
                result.Add(queue.Name);
            }
            return result;
        }
#endif
        #endregion

        #region Tables

        public async Task DeleteTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            if (!UseExists)
            {
                await table.DeleteIfExistsAsync();
            }
            else if (await table.ExistsAsync())
            {
                await table.DeleteAsync();
            }
        }

        public async Task<CloudTable> EnsureTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            if (!UseExists)
            {
                await table.CreateIfNotExistsAsync();
            }
            else if (!await table.ExistsAsync())
            {
                await table.CreateAsync();
            }
            return table;
        }

        public CloudTable GetTable(string name)
        {
            CloudTable table = tableClient.GetTableReference(name);
            return table;
        }

        public async Task<List<string>> ListTablesAsync()
        {
            var result = new List<string>();
            TableContinuationToken continuationToken = null;
            var allTables = new List<CloudTable>();
            do
            {
                var segment = await tableClient.ListTablesSegmentedAsync(continuationToken);
                foreach (var table in segment.Results)
                {
                    result.Add(table.Name);
                }
                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);
            return result;
        }

#if NET461 || NET472
        public List<string> ListTables()
        {
            var result = new List<string>();
            foreach (var table in tableClient.ListTables())
            {
                result.Add(table.Name);
            }
            return result;
        }
#endif

        #endregion

        #region AzureTables
        #endregion
    }

    public static class AzureStorageExtensions
    {
        #region Blobs

        private static async Task DeleteBlobIfExists(CloudBlockBlob blockBlob)
        {
            if (!AzureStorage.UseExists)
            {
                await blockBlob.DeleteIfExistsAsync();
            }
            else if (await blockBlob.ExistsAsync())
            {
                await blockBlob.DeleteAsync();
            }
        }

        public static async Task DeleteBlobAsync(this CloudBlobContainer container, string name)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await DeleteBlobIfExists(blockBlob);
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
                var segment = await dir.ListBlobsSegmentedAsync(false, BlobListingDetails.None, null, token, null, null);
                foreach (var blob in segment.Results)
                {
                    if (blob is CloudBlockBlob)
                    {
                        result.Add(GetFileNameFromBlobURI(blob.Uri, container.Name));
                    }
                }
                token = segment.ContinuationToken;
            }
            while (token != null);
            return result;
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, Stream stream)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await DeleteBlobIfExists(blockBlob);
            await blockBlob.UploadFromStreamAsync(stream);
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, byte[] byteArray)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await DeleteBlobIfExists(blockBlob);
            await blockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
        }

        public static async Task UploadBlobAsync(this CloudBlobContainer container, string name, string content)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            await DeleteBlobIfExists(blockBlob);
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
        public static void CopyFrom<T>(this T destination, T source) where T : AzureTableEntity
        {
            var props = destination.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            foreach (var prop in props)
            {
                if (!prop.Name.Equals("PartitionKey", StringComparison.CurrentCultureIgnoreCase) &&
                    !prop.Name.Equals("RowKey", StringComparison.CurrentCultureIgnoreCase) &&
                    !prop.Name.Equals("Timestamp", StringComparison.CurrentCultureIgnoreCase) &&
                    !prop.Name.Equals("ETag", StringComparison.CurrentCultureIgnoreCase))
                {
                    prop.SetValue(destination, prop.GetValue(source));
                }
            }
        }

        public static async Task<T> DeleteEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
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

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey))).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : AzureTableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey))).Result;
        }

        public static async Task<bool> EntityExistsAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            return await GetEntityAsync(table, entity) != null;
        }

        public static async Task<bool> EntityExistsAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : AzureTableEntity
        {
            return await GetEntityAsync<T>(table, partitionKey, rowKey) != null;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, TableQuery<T> query) where T : AzureTableEntity, new()
        {
            var result = new List<T>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token);
                result.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);
            return result;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table) where T : AzureTableEntity, new()
        {
            return await GetEntitiesAsync(table, new TableQuery<T>());
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, string partitionKey) where T : AzureTableEntity, new()
        {
            var query = new TableQuery<T>();
            query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            return await GetEntitiesAsync(table, query);
        }

        public static async Task<T> InsertEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Insert(entity))).Result;
        }

        public static async Task<T> InsertOrMergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertOrMergeEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrMerge(entity))).Result;
        }

        public static async Task<T> InsertOrReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertOrReplaceEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrReplace(entity))).Result;
        }

        public static async Task<T> MergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
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

        public static async Task<T> ReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
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