using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.CosmosDB.BulkExecutor.BulkDelete;
using Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport;
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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SAEON.Azure.CosmosDB
{
    public class AzureDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class AzureSubDocument { }

    public class AzureCost
    {
        public int NumberOfDocuments { get; set; }
        public double RequestUnitsConsumed { get; set; }
        public TimeSpan Duration { get; set; }

        public AzureCost() { }

        public AzureCost(ResourceResponse<Document> response, Stopwatch stopWatch)
        {
            RequestUnitsConsumed = response.RequestCharge;
            Duration = stopWatch.Elapsed;
        }

        public static AzureCost operator +(AzureCost a, AzureCost b)
        {
            return new AzureCost
            {
                NumberOfDocuments = a.NumberOfDocuments + b.NumberOfDocuments,
                RequestUnitsConsumed = a.RequestUnitsConsumed + b.RequestUnitsConsumed,
                Duration = a.Duration + b.Duration
            };
        }

        public override string ToString()
        {
            if (Duration.TotalSeconds > 0)
            {
                return $"Docs: {NumberOfDocuments:N0} Docs/s: {NumberOfDocuments / Duration.TotalSeconds:N3} Request Units: {RequestUnitsConsumed:N3} RUs/s: {RequestUnitsConsumed / Duration.TotalSeconds:N3} in {Duration}";
            }
            else
            {
                return $"Docs: {NumberOfDocuments:N0} Request Units: {RequestUnitsConsumed:N3} in {Duration}";
            }
        }
    }

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

        public static int DefaultThroughput { get; set; } = 1000;
        public static int DefaultBatchSize { get; set; } = 100000;

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
                    Throughput = int.Parse(ConfigurationManager.AppSettings["AzureCosmosDBThroughput"] ?? DefaultThroughput.ToString());
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

            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId } }))
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

        public async Task LoadDatabaseAsync()
        {
            if (database != null)
            {
                return;
            }

            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId } }))
            {
                try
                {
                    Logging.Verbose("DatabaseUri: {DatabaseUri}", UriFactory.CreateDatabaseUri(DatabaseId));
                    database = await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
                }
                catch (Exception ex)
                {
                    collection = null;
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

            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId }, { "CollectionId", CollectionId }, { "PartitionKey", PartitionKey } }))
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

        public async Task LoadCollectionAsync()
        {
            if (collection != null)
            {
                return;
            }

            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId }, { "CollectionId", CollectionId }, { "PartitionKey", PartitionKey } }))
            {
                try
                {
                    await EnsureDatabaseAsync();
                    collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
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
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "Id", id } }))
            {
                try
                {
                    try
                    {
                        if (AutoEnsureCollection)
                        {
                            await EnsureCollectionAsync();
                        }

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

        public async Task<T> GetItemAsync(object partitionKey, string id)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey }, { "Id", id } }))
            {
                try
                {
                    try
                    {
                        if (AutoEnsureCollection)
                        {
                            await EnsureCollectionAsync();
                        }

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

        public async Task<T> GetItemAsync(Expression<Func<T, object>> partitionKeyExpression, string id, T item)
        {
            return await GetItemAsync(partitionKeyExpression.Compile()(item), id);
        }

        public async Task<T> GetItemAsync(Expression<Func<T, object>> partitionKeyExpression, Expression<Func<T, string>> idExpression, T item)
        {
            return await GetItemAsync(partitionKeyExpression.Compile()(item), idExpression.Compile()(item));
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), new FeedOptions { MaxItemCount = -1 }).Where(predicate).AsDocumentQuery();
                    var results = new List<T>();
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

        #region Create
        public async Task<(T item, AzureCost cost)> CreateItemAsync(T item)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch) { NumberOfDocuments = 1 });
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> CreateItemsAsync(List<T> items)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await CreateItemAsync(item);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> CreateItemAsync(T item, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch) { NumberOfDocuments = 1 });
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> CreateItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            return await CreateItemAsync(item, partitionKeyExpression.Compile()(item));
        }

        public async Task<AzureCost> CreateItemsAsync(List<T> items, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await CreateItemAsync(item, partitionKey);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Update
        public async Task<(T item, AzureCost cost)> UpdateItemAsync(T item)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id), item);
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> UpdateItemsAsync(List<T> items)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await UpdateItemAsync(item);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> UpdateItemAsync(T item, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id), item, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> UpdateItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            return await UpdateItemAsync(item, partitionKeyExpression.Compile()(item));
        }

        public async Task<AzureCost> UpdateItemsAsync(List<T> items, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await UpdateItemAsync(item, partitionKey);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Upsert
        public async Task<(T item, AzureCost cost)> UpsertItemAsync(T item)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> UpsertItemsAsync(List<T> items)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await UpsertItemAsync(item);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> BulkUpsertItemsAsync(List<T> items)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    //foreach (var item in items)
                    //{
                    //    var response = await UpsertItemAsync(item);
                    //    cost.NumberOfDocuments++;
                    //    cost += response.cost;
                    //}
                    var oldMaxRetryWaitTimeInSeconds = client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds;
                    var oldMaxRetryAttemptsOnThrottledRequests = client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests;
                    try
                    {
                        await LoadCollectionAsync();
                        Logging.Verbose("Client: {Client} Database: {Database} Collection: {Collection}", client != null, database != null, collection != null);

                        // Set retry options high for initialization (default values).
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;

                        BulkExecutor bulkExecutor = new BulkExecutor(client, collection);
                        await bulkExecutor.InitializeAsync();

                        // Set retries to 0 to pass control to bulk executor.
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

                        BulkImportResponse bulkImportResponse = null;
                        var tokenSource = new CancellationTokenSource();
                        var token = tokenSource.Token;

                        do
                        {
                            try
                            {
                                bulkImportResponse = await bulkExecutor.BulkImportAsync(
                                    documents: items,
                                    enableUpsert: true,
                                    disableAutomaticIdGeneration: true,
                                    maxConcurrencyPerPartitionKeyRange: null,
                                    maxInMemorySortingBatchSize: null,
                                    cancellationToken: token);
                            }
                            catch (Exception ex)
                            {
                                Logging.Exception(ex);
                                break;
                            }
                        } while (bulkImportResponse.NumberOfDocumentsImported < items.Count);
                        cost.NumberOfDocuments += (int)bulkImportResponse.NumberOfDocumentsImported;
                        cost.RequestUnitsConsumed += bulkImportResponse.TotalRequestUnitsConsumed;
                    }
                    finally
                    {
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = oldMaxRetryWaitTimeInSeconds;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = oldMaxRetryAttemptsOnThrottledRequests;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> UpsertItemAsync(T item, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> UpsertItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            return await UpsertItemAsync(item, partitionKeyExpression.Compile()(item));
        }

        public async Task<AzureCost> UpsertItemsAsync(List<T> items, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await UpsertItemAsync(item, partitionKey);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Delete
        public async Task<(T item, AzureCost cost)> DeleteItemAsync(string id)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "Id", id } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> DeleteItemAsync(T item)
        {
            return await DeleteItemAsync(item.Id);
        }

        public async Task<AzureCost> DeleteItemsAsync(List<T> items)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await DeleteItemAsync(item);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> DeleteItemsAsync(Expression<Func<T, string>> idExpression, Expression<Func<T, bool>> predicate, bool enableCrossPartition = false)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    IQueryable<string> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = enableCrossPartition }).Where(predicate).Select(idExpression);
                    var cost = new AzureCost();
                    foreach (var id in query)
                    {
                        var response = await DeleteItemAsync(id);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }


        public async Task<(T item, AzureCost cost)> DeleteItemAsync(object partitionKey, string id)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey }, { "Id", id }}))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var response = await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    stopWatch.Stop();
                    return ((T)(dynamic)response.Resource, new AzureCost(response, stopWatch));
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, AzureCost cost)> DeleteItemAsync(T item, object partitionKey)
        {
            return await DeleteItemAsync(partitionKey, item.Id);
        }

        public async Task<(T item, AzureCost cost)> DeleteItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            return await DeleteItemAsync(item, partitionKeyExpression.Compile()(item));
        }

        public async Task<AzureCost> DeleteItemsAsync(List<T> items, object partitionKey)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    foreach (var item in items)
                    {
                        var response = await DeleteItemAsync(item, partitionKey);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> DeleteItemsAsync(object partitionKey, Expression<Func<T, string>> idExpression, Expression<Func<T, bool>> predicate, bool enableCrossPartition = false)
        {
            using (Logging.MethodCall<T>(GetType(), new MethodCallParameters { { "PartitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    IQueryable<string> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = enableCrossPartition }).Where(predicate).Select(idExpression);
                    var cost = new AzureCost();
                    foreach (var id in query)
                    {
                        var response = await DeleteItemAsync(partitionKey, id);
                        cost.NumberOfDocuments++;
                        cost += response.cost;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<AzureCost> BulkDeleteItemsAsync(object partitionKey, Expression<Func<T, string>> idExpression, Expression<Func<T, bool>> predicate)
        {
            using (Logging.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureCollection)
                    {
                        await EnsureCollectionAsync();
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var cost = new AzureCost();
                    var oldMaxRetryWaitTimeInSeconds = client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds;
                    var oldMaxRetryAttemptsOnThrottledRequests = client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests;
                    try
                    {
                        await LoadCollectionAsync();
                        IQueryable<string> query = client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                            new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true }).Where(predicate).Select(idExpression);
                        var items = query.AsEnumerable().Select(i => new Tuple<string, string>(partitionKey.ToString(), i)).ToList();
                        Logging.Verbose("Items: {Count} {@Items}", items.Count, items);

                        // Set retry options high for initialization (default values).
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;

                        BulkExecutor bulkExecutor = new BulkExecutor(client, collection);
                        await bulkExecutor.InitializeAsync();

                        // Set retries to 0 to pass control to bulk executor.
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

                        BulkDeleteResponse bulkDeleteResponse = null;
                        try
                        {
                            bulkDeleteResponse = await bulkExecutor.BulkDeleteAsync(items);
                        }
                        catch (Exception ex)
                        {
                            Logging.Exception(ex);
                            throw;
                        }
                        cost.NumberOfDocuments += (int)bulkDeleteResponse.NumberOfDocumentsDeleted;
                        cost.RequestUnitsConsumed += bulkDeleteResponse.TotalRequestUnitsConsumed;
                    }
                    finally
                    {
                        client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = oldMaxRetryWaitTimeInSeconds;
                        client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = oldMaxRetryAttemptsOnThrottledRequests;
                    }
                    stopWatch.Stop();
                    cost.Duration = stopWatch.Elapsed;
                    return cost;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        #endregion

        #endregion
    }

}
