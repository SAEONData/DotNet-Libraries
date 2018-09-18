using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using SAEON.Logs;
using System;
using System.Collections.Generic;
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

    public class AzureCosmosDB
    {
        private DocumentClient documentClient = null;

        private string DatabaseId { get; set; }
        private string CollectionId { get; set; }
        private string PartitionKey { get; set; }
        private int Throughput { get; set; } = 400; 

        public AzureCosmosDB(string databaseId, string collectionId, string partitionKey)
        {
            using (Logging.MethodCall(GetType()))
            {
                try
                {
                    var cosmosDBURL = ConfigurationManager.AppSettings["CosmosDBUrl"];
                    var primaryKey = ConfigurationManager.AppSettings["CosmosDBPrimaryKey"];
                    documentClient = new DocumentClient(new Uri(cosmosDBURL), primaryKey);
                    DatabaseId = databaseId;
                    CollectionId = collectionId;
                    PartitionKey = partitionKey;
                    Throughput = Convert.ToInt32(ConfigurationManager.AppSettings["CosmosDBThroughput"] ?? "400");
                    Logging.Information("CosmosDbUrl: {CosmosDbURL} Database: {DatabaseId} Collection: {CollecionId} PartitionKey: {PartitionKey} Throughput: {Throughput}",
                        cosmosDBURL, DatabaseId, CollectionId, PartitionKey, Throughput);
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
                documentClient = null;
            }
        }

        #region Database
        public async Task EnsureDatabaseAsync()
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "DatabaseId", DatabaseId } }))
            {
                try
                {
                    try
                    {
                        await documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
                    }
                    catch (DocumentClientException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            await documentClient.CreateDatabaseAsync(new Database { Id = DatabaseId });
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
          
        #endregion

        #region Collection
        public async Task EnsureCollectionAsync()
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "DatabaseId", DatabaseId }, { "CollectionId", CollectionId } }))
            {
                try
                {
                    await EnsureDatabaseAsync();
                    try
                    {
                        await documentClient.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
                    }
                    catch (DocumentClientException e)
                    {  
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            var collection = new DocumentCollection { Id = CollectionId };
                            collection.PartitionKey.Paths.Add(PartitionKey);
                            await documentClient.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(DatabaseId), collection, new RequestOptions { OfferThroughput = Throughput });
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
        #endregion

        #region Items
        public async Task<T> GetItemAsync<T>(string id) where T: AzureDocument
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    try
                    {
                        Document document = await documentClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
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

        public async Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : AzureDocument
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    IDocumentQuery<T> query = documentClient.CreateDocumentQuery<T>(
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

        public async Task<Document> CreateItemAsync<T>(T item) where T : AzureDocument
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    return await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<Document> UpdateItemAsync<T>(string id, T item) where T : AzureDocument
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    return await documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task DeleteItemAsync<T>(string id) where T : AzureDocument
        {
            using (Logging.MethodCall<T>(GetType(), new ParameterList { { "Id", id } }))
            {
                try
                {
                    await documentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
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
