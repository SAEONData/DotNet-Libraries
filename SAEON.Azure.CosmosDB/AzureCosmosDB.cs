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
        public DateTime Date { get; set; } = DateTime.MinValue;
        public int Epoch
        {
            get
            {
                return ((Date == null) || (Date == DateTime.MinValue)) ? int.MinValue : Date.ToEpoch();
            }
        }

        public EpochDate() { }
        public EpochDate(DateTime date)
        {
            Date = date;
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

        public AzureCosmosDB(string databaseId, string collectionId, string partitionKey)
        {
            using (Logging.MethodCall(GetType()))
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
            using (Logging.MethodCall(GetType()))
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

            using (Logging.MethodCall(GetType(), new ParameterList { { "DatabaseId", DatabaseId } }))
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

            using (Logging.MethodCall(GetType(), new ParameterList { { "DatabaseId", DatabaseId }, { "CollectionId", CollectionId }, { "PartitionKey", PartitionKey } }))
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
                        collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        {
                            Path = $"/{prop.Name}/Epoch/?",
                            Indexes = new Collection<Index> { { new RangeIndex(DataType.Number, -1) } }
                        });
                    };
                    foreach (var subProp in typeof(T).GetProperties().Where(i => i.PropertyType == typeof(AzureSubDocument)))
                    {
                        foreach (var prop in subProp.GetType().GetProperties().Where(i => i.PropertyType == typeof(EpochDate)))
                        {
                            collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                            {
                                Path = $"/{subProp.Name}/{prop.Name}/Epoch/?",
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
                        await EnsureCollectionAsync();
                        Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                        return (T)(dynamic)document;
                    }
                    catch (DocumentClientException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            return default(T);
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

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    await EnsureCollectionAsync();
                    IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), new FeedOptions { MaxItemCount = -1 }).Where(predicate).AsDocumentQuery();
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
                    await EnsureCollectionAsync();
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
                    await EnsureCollectionAsync();
                    return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    await EnsureCollectionAsync();
                    await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
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
