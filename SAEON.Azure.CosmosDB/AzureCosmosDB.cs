using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using SAEON.Core.Extensions;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace SAEON.Azure.CosmosDB
{
    public class AzureDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class AzureSubDocument { }

    public class EpochDate
    {
        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; } = DateTime.MinValue;
        [JsonProperty("epoch")]
        public int Epoch
        {
            get
            {
                return ((DateTime == null) || (DateTime == DateTime.MinValue)) ? int.MinValue : DateTime.ToEpoch();
            }
        }

        public EpochDate() { }
        public EpochDate(DateTime dateTime)
        {
            DateTime = dateTime;
        }
    }

    public class AzureCosmosDB<T> where T : AzureDocument
    {
        private DocumentClient client = null;
        private Database database = null;
        private DocumentCollection collection = null;

        private const int DefaultThroughput = 1000;
        private string DatabaseId { get; set; }
        private string CollectionId { get; set; }
        private string PartitionKey { get; set; }
        private int Throughput { get; set; } = DefaultThroughput;

        public static bool AutoEnsureCollection { get; set; } = false;

        public AzureCosmosDB(string databaseId, string collectionId, string partitionKey)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    var cosmosDBUrl = ConfigurationManager.AppSettings["AzureCosmosDBUrl"];
                    if (string.IsNullOrWhiteSpace(cosmosDBUrl))
                    {
                        throw new ArgumentNullException("AppSettings.AzureCosmosDBUrl cannot be null");
                    }

                    var authKey = ConfigurationManager.AppSettings["AzureCosmosDBAuthKey"];
                    if (string.IsNullOrWhiteSpace(authKey))
                    {
                        throw new ArgumentNullException("AppSettings.AzureCosmosDBAuthKey cannot be null");
                    }

                    client = new DocumentClient(new Uri(cosmosDBUrl), authKey);
                    DatabaseId = databaseId;
                    CollectionId = collectionId;
                    PartitionKey = partitionKey;
                    Throughput = Convert.ToInt32(ConfigurationManager.AppSettings["AzureCosmosDBThroughput"] ?? DefaultThroughput.ToString());
                    Logging.Information("CosmosDbUrl: {CosmosDbUrl} Database: {DatabaseId} Collection: {CollectionId} PartitionKey: {PartitionKey} Throughput: {Throughput}",
                        cosmosDBUrl, DatabaseId, CollectionId, PartitionKey, Throughput);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        ~AzureCosmosDB()
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                collection = null;
                database = null;
                client = null;
            }
        }

        #region Database
        public async Task EnsureDatabaseAsync()
        {
            if (database != null)
            {
                return;
            }

            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "DatabaseId", DatabaseId } }))
            {
                try
                {
                    database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });
                }
                catch (Exception ex)
                {
                    database = null;
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        #endregion

        #region Collection
        public async Task EnsureCollectionAsync()
        {
            if (collection != null)
            {
                return;
            }

            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "DatabaseId", DatabaseId }, { "CollectionId", CollectionId }, { "PartitionKey", PartitionKey } }))
            {
                try
                {
                    await EnsureDatabaseAsync();
                    var collection = new DocumentCollection { Id = CollectionId };
                    collection.PartitionKey.Paths.Add(PartitionKey);
                    collection.IndexingPolicy.IndexingMode = IndexingMode.Lazy;
                    // Defaults
                    collection.IndexingPolicy.IncludedPaths.Add(
                        new IncludedPath
                        {
                            Path = "/*",
                            Indexes = new Collection<Index> {
                                new HashIndex(DataType.String) { Precision = 3 },
                                new RangeIndex(DataType.Number) { Precision = -1 }
                            }
                        });
                    foreach (var prop in typeof(T).GetProperties().Where(i => i.PropertyType == typeof(EpochDate)))
                    {
                        var propName = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                        collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        {
                            Path = $"/{propName}/epoch/?",
                            Indexes = new Collection<Index> { { new RangeIndex(DataType.Number, -1) } }
                        });
                    };
                    foreach (var subProp in typeof(T).GetProperties().Where(i => i.PropertyType == typeof(AzureSubDocument)))
                    {
                        var subPropName = subProp.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? subProp.Name;
                        foreach (var prop in subProp.GetType().GetProperties().Where(i => i.PropertyType == typeof(EpochDate)))
                        {
                            var propName = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                            collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                            {
                                Path = $"/{subPropName}/{propName}/epoch/?",
                                Indexes = new Collection<Index> { { new RangeIndex(DataType.Number, -1) } }
                            });
                        }
                    };
                    this.collection = await client.CreateDocumentCollectionIfNotExistsAsync(database.SelfLink, collection);
                }
                catch (Exception ex)
                {
                    collection = null;
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Items
        public async Task<T> GetItemAsync(string id)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    try
                    {
                        if (AutoEnsureCollection) await EnsureCollectionAsync();
                        Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                        return (T)(dynamic)document;
                    }
                    catch (DocumentClientException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            Logging.Verbose("Item iwth ID {ID} not found", id);
                            return default;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<T> GetItemAsync(string id, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id }, { "PartitionKey", partitionKey } }))
            {
                try
                {
                    try
                    {
                        if (AutoEnsureCollection) await EnsureCollectionAsync();
                        Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                        return (T)(dynamic)document;
                    }
                    catch (DocumentClientException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            Logging.Verbose("Item iwth ID {ID}, PartitionKey {PartitionKey} not found", id, partitionKey);
                            return default;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<T> GetItemAsync(string id, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await GetItemAsync(id, partitionKeyExpression.Compile()(item));
        }

        public async Task<T> GetItemAsync(Expression<Func<T, string>> idExpression, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await GetItemAsync(idExpression.Compile()(item), partitionKeyExpression.Compile()(item));
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    IDocumentQuery<T> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), new FeedOptions { MaxItemCount = -1 }).Where(predicate).AsDocumentQuery();
                    List<T> results = new List<T>();
                    while (query.HasMoreResults)
                    {
                        results.AddRange(await query.ExecuteNextAsync<T>());
                    }
                    return results;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> CreateItemAsync(T item)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> UpdateItemAsync(string id, T item)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> UpdateItemAsync(Expression<Func<T, string>> idExpression, T item)
        {
            return await UpdateItemAsync(idExpression.Compile()(item), item);
        }


        public async Task<Document> UpsertItemAsync(string id, T item)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> UpsertItemAsync(Expression<Func<T, string>> idExpression, T item)
        {
            return await UpsertItemAsync(idExpression.Compile()(item), item);
        }

        public async Task<Document> UpsertItemAsync(string id, object partitionKey, T item)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id }, { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> UpsertItemAsync(string id, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await UpsertItemAsync(id, partitionKeyExpression.Compile()(item), item);
        }

        public async Task<Document> UpsertItemAsync(Expression<Func<T, string>> idExpression, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await UpsertItemAsync(idExpression.Compile()(item), partitionKeyExpression.Compile()(item), item);
        }

        public async Task<Document> DeleteItemAsync(string id)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> DeleteItemAsync(string id, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id }, { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    return await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> DeleteItemAsync(string id, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await DeleteItemAsync(id, partitionKeyExpression.Compile()(item));
        }

        public async Task<Document> DeleteItemAsync(Expression<Func<T, string>> idExpression, Expression<Func<T, object>> partitionKeyExpression, T item)
        {
            return await DeleteItemAsync(idExpression.Compile()(item), partitionKeyExpression.Compile()(item));
        }

        public async Task DeleteItemsAsync(Expression<Func<T, string>> idExpression, Expression<Func<T, bool>> predicate, bool enableCrossPartition = false)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    IQueryable<string> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = enableCrossPartition }).Where(predicate).Select(idExpression);
                    foreach (var id in query)
                    {
                        await DeleteItemAsync(id);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task DeleteItemsAsync(Expression<Func<T, string>> idExpression, object partitionKey, Expression<Func<T, bool>> predicate, bool enableCrossPartition = false)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection) await EnsureCollectionAsync();
                    IQueryable<string> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = enableCrossPartition }).Where(predicate).Select(idExpression);
                    foreach (var id in query)
                    {
                        await DeleteItemAsync(id, partitionKey);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        #endregion
    }

}
