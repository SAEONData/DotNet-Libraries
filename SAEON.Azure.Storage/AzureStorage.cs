using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private BlobServiceClient blobServiceClient;
        private QueueServiceClient queueServiceClient;
        private CloudStorageAccount cloudStorageAccount;
        private CloudTableClient cloudTableClient;
        public static bool UseExists { get; set; }

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
                return await blobContainerClient.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else if (await blobContainerClient.ExistsAsync().ConfigureAwait(false))
            {
                await blobContainerClient.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task EnsureContainerAsync(string name)
        {
            var blobContainerClient = GetBlobContainerClient(name);
            if (!UseExists)
            {
                await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            else if (!await blobContainerClient.ExistsAsync().ConfigureAwait(false))
            {
                await blobContainerClient.CreateAsync().ConfigureAwait(false);
            }
        }

        public BlobContainerClient GetBlobContainerClient(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return blobServiceClient.GetBlobContainerClient(name.ToLower());
        }
        #endregion Containers

        #region Blobs
        public async Task DeleteBlobAsync(BlobContainerClient blobContainerClient, string name)
        {
            if (blobContainerClient == null) throw new ArgumentNullException(nameof(blobContainerClient));
            await blobContainerClient.DeleteBlobIfExistsAsync(name).ConfigureAwait(false);
        }

        public static async Task DownloadBlobAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            if (blobContainerClient == null) throw new ArgumentNullException(nameof(blobContainerClient));
            var blobClient = blobContainerClient.GetBlobClient(name);
            await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
        }

        public static async Task<List<string>> ListFolders(BlobContainerClient blobContainerClient, string folder)
        {
            if (blobContainerClient == null) throw new ArgumentNullException(nameof(blobContainerClient));
            return await blobContainerClient.ListFolder(folder).ConfigureAwait(false);
        }

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            if (blobContainerClient == null) throw new ArgumentNullException(nameof(blobContainerClient));
            await blobContainerClient.UploadBlobAsync(name, stream).ConfigureAwait(false);
        }

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await UploadBlobAsync(blobContainerClient, name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobAsync(BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await UploadBlobAsync(blobContainerClient, name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            if (blobContainerClient == null) throw new ArgumentNullException(nameof(blobContainerClient));
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                await blobClient.UploadAsync(stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await UploadBlobIfNotExistsAsync(blobContainerClient, name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await UploadBlobIfNotExistsAsync(blobContainerClient, name, stream).ConfigureAwait(false);
            }
        }

        #endregion Blobs

        #region Queues
        public async Task DeleteQueueAsync(string name)
        {
            var queueClient = GetQueueClient(name);
            if (!UseExists)
            {
                await queueClient.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else if (await queueClient.ExistsAsync().ConfigureAwait(false))
            {
                await queueClient.DeleteAsync().ConfigureAwait(false);
            }
        }

        public async Task EnsureQueueAsync(string name)
        {
            var queueClient = GetQueueClient(name);
            if (!UseExists)
            {
                await queueClient.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            else if (!await queueClient.ExistsAsync().ConfigureAwait(false))
            {
                await queueClient.CreateAsync().ConfigureAwait(false);
            }
        }

        public QueueClient GetQueueClient(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return queueServiceClient.GetQueueClient(name.ToLower());
        }

        public List<string> ListQueues()
        {
            var result = new List<string>();
            foreach (var queue in queueServiceClient.GetQueues())
            {
                result.Add(queue.Name);
            }
            return result;
        }

        public async Task<List<string>> ListQueuesAsync()
        {
            var result = new List<string>();
            var allQueues = queueServiceClient.GetQueuesAsync();
            IAsyncEnumerator<QueueItem> enumerator = allQueues.GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    result.Add(enumerator.Current.Name);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            return result;
        }

        public bool QueueExists(string name)
        {
            return ListQueues().Any(i => i == name.ToLower());
        }

        public async Task<bool> QueueExistsAsync(string name)
        {
            return (await ListQueuesAsync().ConfigureAwait(false)).Any(i => i == name.ToLower());
        }
        #endregion

        #region Tables

        public async Task DeleteTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            if (!UseExists)
            {
                await table.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else if (await table.ExistsAsync().ConfigureAwait(false))
            {
                await table.DeleteAsync().ConfigureAwait(false);
            }
        }

        public async Task<CloudTable> EnsureTableAsync(string name)
        {
            CloudTable table = GetTable(name);
            if (!UseExists)
            {
                await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            else if (!await table.ExistsAsync().ConfigureAwait(false))
            {
                await table.CreateAsync().ConfigureAwait(false);
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
            do
            {
                var segment = await cloudTableClient.ListTablesSegmentedAsync(continuationToken).ConfigureAwait(false);
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
                await blobContainerClient.DeleteBlobIfExistsAsync(name).ConfigureAwait(false);
            }
            else
            {
                var blobClient = blobContainerClient.GetBlobClient(name);
                if (await blobClient.ExistsAsync().ConfigureAwait(false))
                {
                    await blobClient.DeleteAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<bool> DownloadBlobAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync().ConfigureAwait(false)) return false;
            await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
            return true;
        }

        public static async Task<List<string>> ListFolder(this BlobContainerClient blobContainerClient, string folder)
        {
            var blobs = blobContainerClient.GetBlobsByHierarchyAsync(prefix: folder);
            var result = new List<string>();
            await foreach (var blob in blobs)
            {
                if (blob.IsBlob)
                {
                    result.Add(blob.Blob.Name);
                }
            }
            return result;
        }

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            await blobContainerClient.DeleteBlobIfExistsAsync(name).ConfigureAwait(false);
            await blobContainerClient.UploadBlobAsync(name, stream).ConfigureAwait(false);
        }

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await blobContainerClient.UploadBlobAsync(name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobAsync(this BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await blobContainerClient.UploadBlobAsync(name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, Stream stream)
        {
            var blobClient = blobContainerClient.GetBlobClient(name);
            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                await blobClient.UploadAsync(stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                await blobContainerClient.UploadBlobIfNotExistsAsync(name, stream).ConfigureAwait(false);
            }
        }

        public static async Task UploadBlobIfNotExistsAsync(this BlobContainerClient blobContainerClient, string name, string content)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
            {
                await blobContainerClient.UploadBlobIfNotExistsAsync(name, stream).ConfigureAwait(false);
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
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            T oldEntity = await GetEntityAsync(table, entity).ConfigureAwait(false);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("DeleteEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            return (T)(await table.ExecuteAsync(TableOperation.Delete(oldEntity)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : AzureTableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey)).ConfigureAwait(false)).Result;
        }

        public static async Task<bool> EntityExistsAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            return await GetEntityAsync(table, entity).ConfigureAwait(false) != null;
        }

        public static async Task<bool> EntityExistsAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : AzureTableEntity
        {
            return await GetEntityAsync<T>(table, partitionKey, rowKey).ConfigureAwait(false) != null;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, TableQuery<T> query) where T : AzureTableEntity, new()
        {
            var result = new List<T>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                result.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);
            return result;
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table) where T : AzureTableEntity, new()
        {
            return await GetEntitiesAsync(table, new TableQuery<T>()).ConfigureAwait(false);
        }

        public static async Task<List<T>> GetEntitiesAsync<T>(this CloudTable table, string partitionKey) where T : AzureTableEntity, new()
        {
            var query = new TableQuery<T>();
            query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            return await GetEntitiesAsync(table, query).ConfigureAwait(false);
        }

        public static async Task<T> InsertEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null)
            {
                throw new NullReferenceException("InsertEntity: Null entity");
            }
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Insert(entity)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> InsertOrMergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrMerge(entity)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> InsertOrReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrReplace(entity)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> MergeEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            T oldEntity = await GetEntityAsync(table, entity).ConfigureAwait(false);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("MergeEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            oldEntity.CopyFrom(entity);
            oldEntity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Merge(oldEntity)).ConfigureAwait(false)).Result;
        }

        public static async Task<T> ReplaceEntityAsync<T>(this CloudTable table, T entity) where T : AzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            T oldEntity = await GetEntityAsync(table, entity).ConfigureAwait(false);
            if (oldEntity == null)
            {
                throw new KeyNotFoundException(string.Format("ReplaceEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            }

            oldEntity.CopyFrom(entity);
            oldEntity.SetKeys();
            return (T)(await table.ExecuteAsync(TableOperation.Replace(oldEntity)).ConfigureAwait(false)).Result;
        }

        #endregion
    }
}