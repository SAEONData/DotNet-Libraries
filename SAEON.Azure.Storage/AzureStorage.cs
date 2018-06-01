using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection; 
using System.Threading.Tasks;

namespace SAEON.Azure.Storage
{
    public class AzureStorage
    {
        CloudStorageAccount storageAccount = null; 
        CloudBlobClient blobClient = null; 
        CloudQueueClient queueClient = null;
        CloudTableClient tableClient = null;

        public AzureStorage() : this("AzureStorage") { }

        public AzureStorage(string connectionStringName)
        {
            //using (Logging.MethodCall(GetType(), new ParameterList { { nameof(connectionStringName), connectionStringName } }))
            {
                var connectionString = ConfigurationManager.AppSettings[connectionStringName];
                Console.WriteLine($"ConnectionString: {connectionStringName} {connectionString}");
                //Logging.Information("ConnectionString: {connectionStringName} {ConnectionString}", connectionStringName, connectionString);
                storageAccount = CloudStorageAccount.Parse(connectionString);
                blobClient = storageAccount.CreateCloudBlobClient();
                queueClient = storageAccount.CreateCloudQueueClient();
                tableClient = storageAccount.CreateCloudTableClient();
            }
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
        #endregion

        #region Blobs
        public async Task DeleteBlobAsync(CloudBlobContainer container, string name)
        {
            await container.DeleteBlobAsync(name);
        }

        public static async Task<bool> DownloadBlobAsync(CloudBlobContainer container, string name, Stream stream)
        {
            return await container.DownloadBlobAsync(name, stream);
        }

        public static async Task UploadBlobAsync(CloudBlobContainer container, string name, Stream stream)
        {
            await container.UploadBlobAsync(name, stream);
        }

        public static async Task UploadBlobAsync(CloudBlobContainer container, string name, byte[] byteArray)
        {
            await container.UploadBlobAsync(name, byteArray);
        }

        public static async Task UploadBlobIfNotExistsAsync(CloudBlobContainer container, string name, Stream stream)
        {
            await container.UploadBlobIfNotExistsAsync(name, stream);
        }

        public static async Task UploadBlobIfNotExistsAsync(CloudBlobContainer container, string name, byte[] byteArray)
        {
            await container.UploadBlobIfNotExistsAsync(name, byteArray);
        }
        #endregion

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

        #region TableEntities
        //public async Task<T> DeleteEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.DeleteEntityAsync(entity); 
        //}

        //public async Task<T> GetEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.GetEntityAsync(entity);
        //}

        //public async Task<T> GetEntityAsync<T>(CloudTable table, string partitionKey, string rowKey) where T : TableEntity
        //{
        //    return await table.GetEntityAsync<T>(partitionKey, rowKey);
        //}

        //public async Task<T> InsertEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.InsertEntityAsync(entity);
        //}

        //public async Task<T> InsertOrMergeEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.InsertOrMergeEntityAsync(entity);
        //}

        //public async Task<T> InsertOrReplaceEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.InsertOrReplaceEntityAsync(entity);
        //}

        //public async Task<T> MergeEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.MergeEntityAsync(entity);
        //}

        //public async Task<T> ReplaceEntityAsync<T>(CloudTable table, T entity) where T : TableEntity
        //{
        //    return await table.ReplaceEntityAsync(entity);
        //}
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
            if (!await blockBlob.ExistsAsync()) return false;
            await blockBlob.DownloadToStreamAsync(stream);
            return true;
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

        public static async Task UploadBlobIfNotExistsAsync(this CloudBlobContainer container, string name, Stream stream)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
                await blockBlob.UploadFromStreamAsync(stream);
        }

        public static async Task UploadBlobIfNotExistsAsync(this CloudBlobContainer container, string name, byte[] byteArray)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
                await blockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
        }
        #endregion

        #region Queues
        #endregion

        #region Tables
        #endregion

        #region TableEntities

        public static void CopyFrom(this TableEntity destination, TableEntity source)
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
        public static async Task<T> DeleteEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("DeleteEntity: Null entity");
            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null) throw new KeyNotFoundException(string.Format("DeleteEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            return (T)(await table.ExecuteAsync(TableOperation.Delete(oldEntity))).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey))).Result;
        }

        public static async Task<T> GetEntityAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : TableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey))).Result;
        }

        public static async Task<T> InsertEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("InsertEntity: Null entity");
            return (T)(await table.ExecuteAsync(TableOperation.Insert(entity))).Result;
        }

        public static async Task<T> InsertOrMergeEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("InsertOrMergeEntity: Null entity");
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrMerge(entity))).Result;
        } 

        public static async Task<T> InsertOrReplaceEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("InsertOrReplaceEntity: Null entity");
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrReplace(entity))).Result;
        } 

        public static async Task<T> MergeEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("MergeEntity: Null entity");
            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null) throw new KeyNotFoundException(string.Format("MergeEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            oldEntity.CopyFrom(entity);
            return (T)(await table.ExecuteAsync(TableOperation.Merge(oldEntity))).Result;
        }

        public static async Task<T> ReplaceEntityAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            if (entity == null) throw new NullReferenceException("ReplaceEntity: Null entity");
            T oldEntity = await GetEntityAsync<T>(table, entity);
            if (oldEntity == null) throw new KeyNotFoundException(string.Format("ReplaceEntity: Unable to find p:[{0}] r:[{1}]", entity.PartitionKey, entity.RowKey));
            oldEntity.CopyFrom(entity);
            return (T)(await table.ExecuteAsync(TableOperation.Replace(oldEntity))).Result;
        }
        #endregion
    }

}
