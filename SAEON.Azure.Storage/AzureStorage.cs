using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
        private BlobServiceClient blobServiceClient = null;
        private QueueServiceClient queueServiceClient = null;
        private CloudStorageAccount cloudStorageAccount = null;
        private CloudTableClient cloudTableClient = null;
        public static bool UseExists { get; set; } = false;

        public AzureStorage(string connectionString)
        {
            blobServiceClient = new BlobServiceClient(connectionString);
            queueServiceClient = new QueueServiceClient(connectionString);
            cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
        }

        ~AzureStorage()
        {
            blobServiceClient = null;
            queueServiceClient = null;
            cloudTableClient = null;
            cloudStorageAccount = null;
        }

        #region Containers

        public async Task<bool> DeleteContainerAsync(string name)
        {
            var blobContainerClient = GetBlobContainerClient(name);
            if (!UseExists)
            {
                return await blobContainerClient.DeleteIfExistsAsync();
            }
            else if (await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.DeleteAsync();
                return true;
            }
            return false;
        }

        public async Task EnsureContainerAsync(string name)
        {
            var blobContainerClient = GetBlobContainerClient(name);
            if (!UseExists)
            {
                await blobContainerClient.CreateIfNotExistsAsync();
            }
            else if (!await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.CreateAsync();
            }
        }

        public BlobContainerClient GetBlobContainerClient(string name)
        {
            return blobServiceClient.GetBlobContainerClient(name.ToLower());
        }
        #endregion Containers

        #region Blobs
        public async Task DeleteBlobAsync(BlobContainerClient blobContainerClient, string name)
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(name);
        }

        public static async Task DownloadBlobAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            await blobClient.DownloadToAsync(stream);
        }

        //public static async Task<List<string>> FolderList(CloudBlobContainer container, string folder)
        //{
        //    return await container.FolderList(folder);
        //}

        //public static async Task<List<string>> FolderList(BlobContainerClient blobContainerClient, string folder)

        //{
        //    blobContainerClient.GetBlobsByHierarchyAsync()
        //    return await container.FolderList(folder);
        //}

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            await blobContainerClient.UploadBlobAsync(name, stream);
        }

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await UploadBlobAsync(blobContainerClient, name, stream);
            }
        }

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await UploadBlobAsync(blobContainerClient, name, stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync())
            {
                await blobClient.UploadAsync(stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await UploadBlobIfNotExistsAsync(blobContainerClient, name, stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await UploadBlobIfNotExistsAsync(blobContainerClient, name, stream);
            }
        }

        #endregion Blobs

        #region Queues
        public async Task DeleteQueueAsync(string name)
        {
            var queueClient = GetQueueClient(name);
            if (!UseExists)
            {
                await queueClient.DeleteIfExistsAsync();
            }
            else if (await queueClient.ExistsAsync())
            {
                await queueClient.DeleteAsync();
            }
        }

        public async Task EnsureQueueAsync(string name)
        {
            var queueClient = GetQueueClient(name);
            if (!UseExists)
            {
                await queueClient.CreateIfNotExistsAsync();
            }
            else if (!await queueClient.ExistsAsync())
            {
                await queueClient.CreateAsync();
            }
        }

        public QueueClient GetQueueClient(string name)
        {
            return queueServiceClient.GetQueueClient(name.ToLower());
        }

        //public async Task<List<string>> ListQueuesAsync()
        //{
        //    var result = new List<string>();
        //    QueueContinuationToken continuationToken = null;
        //    var allTables = new List<CloudTable>();
        //    do
        //    {
        //        var segment = await queueClient.ListQueuesSegmentedAsync(continuationToken);
        //        foreach (var queue in segment.Results)
        //        {
        //            result.Add(queue.Name);
        //        }
        //        continuationToken = segment.ContinuationToken;
        //    }
        //    while (continuationToken != null);
        //    return result;
        //}

        public List<string> ListQueues()
        {
            var result = new List<string>();
            foreach (var queue in queueServiceClient.GetQueues())
            {
                result.Add(queue.Name);
            }
            return result;
        }
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
            CloudTable table = cloudTableClient.GetTableReference(name);
            return table;
        }

        public async Task<List<string>> ListTablesAsync()
        {
            var result = new List<string>();
            TableContinuationToken continuationToken = null;
            var allTables = new List<CloudTable>();
            do
            {
                var segment = await cloudTableClient.ListTablesSegmentedAsync(continuationToken);
                foreach (var table in segment.Results)
                {
                    result.Add(table.Name);
                }
                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);
            return result;
        }

        public List<string> ListTables()
        {
            var result = new List<string>();
            foreach (var table in cloudTableClient.ListTables())
            {
                result.Add(table.Name);
            }
            return result;
        }

        #endregion

        #region AzureTables
        #endregion
    }

    public static class AzureStorageExtensions
    {

        #region Blobs
        public static async Task DeleteBlobAsync(this BlobContainerClient blobContainerClient, string name)
        {
            if (!AzureStorage.UseExists)
            {
                await blobContainerClient.DeleteBlobIfExistsAsync(name);
            }
            else
            {
                var blobClient = blobContainerClient.GetBlobClient(name);
                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                }
            }
        }

        public static async Task<bool> DownloadBlobAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync()) return false;
            await blobClient.DownloadToAsync(stream);
            return true;
        }

        //public static async Task<List<string>> FolderList(this CloudBlobContainer container, string folder)
        //{
        //    string GetFileNameFromBlobURI(Uri theUri, string containerName)
        //    {
        //        string theFile = theUri.ToString();
        //        int dirIndex = theFile.IndexOf(containerName);
        //        string oneFile = theFile.Substring(dirIndex + containerName.Length + 1,
        //            theFile.Length - (dirIndex + containerName.Length + 1));
        //        return oneFile;
        //    }

        //    var result = new List<string>();
        //    BlobContinuationToken token = null;
        //    do
        //    {
        //        var dir = container.GetDirectoryReference(folder);
        //        var segment = await dir.ListBlobsSegmentedAsync(false, BlobListingDetails.None, null, token, null, null);
        //        foreach (var blob in segment.Results)
        //        {
        //            if (blob is CloudBlockBlob)
        //            {
        //                result.Add(GetFileNameFromBlobURI(blob.Uri, container.Name));
        //            }
        //        }
        //        token = segment.ContinuationToken;
        //    }
        //    while (token != null);
        //    return result;
        //}

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(name);
            await blobContainerClient.UploadBlobAsync(name, stream);
        }

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await blobContainerClient.UploadBlobAsync(name, stream);
            }
        }

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await blobContainerClient.UploadBlobAsync(name, stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync())
            {
                await blobClient.UploadAsync(stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await blobContainerClient.UploadBlobIfNotExistsAsync(name, stream);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await blobContainerClient.UploadBlobIfNotExistsAsync(name, stream);
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