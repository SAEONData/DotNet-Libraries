using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using SAEON.Core;
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
    public class CosmosDBItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    //public class AzureSubDocument { }

    public class CosmosDBCost<T> where T : CosmosDBItem
    {
        public int NumberOfItems { get; set; }
        public double RequestUnitsConsumed { get; set; }
        public TimeSpan Duration { get; set; }

        public CosmosDBCost() { }

        public CosmosDBCost(ItemResponse<T> response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            NumberOfItems = 1;
            RequestUnitsConsumed = response.RequestCharge;
            Duration = response.Diagnostics.GetClientElapsedTime();
        }

        public CosmosDBCost(FeedResponse<T> response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            NumberOfItems = response.Count;
            RequestUnitsConsumed = response.RequestCharge;
            Duration = response.Diagnostics.GetClientElapsedTime();
        }

        public static CosmosDBCost<T> operator +(CosmosDBCost<T> left, CosmosDBCost<T> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            return new CosmosDBCost<T>
            {
                NumberOfItems = left.NumberOfItems + right.NumberOfItems,
                RequestUnitsConsumed = left.RequestUnitsConsumed + right.RequestUnitsConsumed,
                Duration = left.Duration + right.Duration
            };
        }

        public static CosmosDBCost<T> Add(CosmosDBCost<T> left, CosmosDBCost<T> right)
        {
            return left + right;
        }

        public override string ToString()
        {
            if (Duration.TotalSeconds > 0)
            {
                return $"Items: {NumberOfItems:N0} Items/s: {NumberOfItems / Duration.TotalSeconds:N3} Request Units: {RequestUnitsConsumed:N3} RUs/s: {RequestUnitsConsumed / Duration.TotalSeconds:N3} in {Duration.TimeStr()}";
            }
            else
            {
                return $"Items: {NumberOfItems:N0} Request Units: {RequestUnitsConsumed:N3} in {Duration.TimeStr()}";
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

    public class AzureCosmosDB<T> where T : CosmosDBItem
    {
        private CosmosClient client = null;
        private Database database = null;
        private Container container = null;

        public static int DefaultThroughput { get; set; } = 1000;
        public static int DefaultBatchSize { get; set; } = 100000;
        public static int DefaultRetries { get; set; } = 500;
        public static int DefaultRetryWaitSecs { get; set; } = 60;

        private string DatabaseId { get; set; }
        private string ContainerId { get; set; }
        private string PartitionKey { get; set; }
        private int Throughput { get; set; } = DefaultThroughput;

        public static bool AutoEnsureContainer { get; set; } = true;

        public AzureCosmosDB(string databaseId, string containerId, string partitionKey, bool allowBulkExecution = false)
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    var cosmosDBUrl = ConfigurationManager.AppSettings["AzureCosmosDBUrl"];
                    if (string.IsNullOrWhiteSpace(cosmosDBUrl))
                    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                        throw new ArgumentNullException("AppSettings.AzureCosmosDBUrl cannot be null");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
                    }

                    var authKey = ConfigurationManager.AppSettings["AzureCosmosDBAuthKey"];
                    if (string.IsNullOrWhiteSpace(authKey))
                    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                        throw new ArgumentNullException("AppSettings.AzureCosmosDBAuthKey cannot be null");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
                    }
                    var clientOptions = new CosmosClientOptions
                    {
                        AllowBulkExecution = allowBulkExecution,
                        MaxRetryAttemptsOnRateLimitedRequests = int.Parse(ConfigurationManager.AppSettings["AzureCosmosDBRetries"] ?? DefaultRetries.ToString()),
                        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["AzureCosmosDBRetryWaitSecs"] ?? DefaultRetryWaitSecs.ToString()))
                    };
                    client = new CosmosClient(cosmosDBUrl, authKey, clientOptions);
                    DatabaseId = databaseId;
                    ContainerId = containerId;
                    PartitionKey = partitionKey;
                    Throughput = int.Parse(ConfigurationManager.AppSettings["AzureCosmosDBThroughput"] ?? DefaultThroughput.ToString());
                    Logger.Information("CosmosDbUrl: {CosmosDbUrl} Database: {DatabaseId} Container: {ContainerId} PartitionKey: {PartitionKey} Throughput: {Throughput} BulkExecution: {BulkExection}",
                        cosmosDBUrl, DatabaseId, ContainerId, PartitionKey, Throughput, allowBulkExecution);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        ~AzureCosmosDB()
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                container = null;
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

            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId } }))
            {
                try
                {
                    database = await client.CreateDatabaseIfNotExistsAsync(DatabaseId);
                }
                catch (Exception ex)
                {
                    database = null;
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        //public async Task LoadDatabaseAsync()
        //{
        //    if (database != null)
        //    {
        //        return;
        //    }

        //    using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId } }))
        //    {
        //        try
        //        {
        //            Logger.Verbose("DatabaseUri: {DatabaseUri}", UriFactory.CreateDatabaseUri(DatabaseId));
        //            database = await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
        //        }
        //        catch (Exception ex)
        //        {
        //            container = null;
        //            database = null;
        //            Logger.Exception(ex);
        //            throw;
        //        }
        //    }
        //}
        #endregion

        #region Container
        public async Task EnsureContainerAsync()
        {
            if (container != null)
            {
                return;
            }

            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "DatabaseId", DatabaseId }, { "ContainerId", ContainerId }, { "PartitionKey", PartitionKey } }))
            {
                try
                {
                    await EnsureDatabaseAsync();
                    var containerProperties = new ContainerProperties(ContainerId, PartitionKey);
                    containerProperties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
                    //// Defaults
                    //Container.IndexingPolicy.IncludedPaths.Add(
                    //    new IncludedPath
                    //    {
                    //        Path = "/*",
                    //        Indexes = new Container<Index> {
                    //            new HashIndex(DataType.String) { Precision = 3 },
                    //            new RangeIndex(DataType.Number) { Precision = -1 }
                    //        }
                    //    });
                    //foreach (var prop in typeof(T).GetProperties().Where(i => i.PropertyType == typeof(EpochDate)))
                    //{
                    //    var propName = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                    //    Container.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                    //    {
                    //        Path = $"/{propName}/epoch/?",
                    //        Indexes = new Container<Index> { { new RangeIndex(DataType.Number, -1) } }
                    //    });
                    //};
                    //foreach (var subProp in typeof(T).GetProperties().Where(i => i.PropertyType == typeof(AzureSubDocument)))
                    //{
                    //    var subPropName = subProp.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? subProp.Name;
                    //    foreach (var prop in subProp.GetType().GetProperties().Where(i => i.PropertyType == typeof(EpochDate)))
                    //    {
                    //        var propName = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                    //        Container.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                    //        {
                    //            Path = $"/{subPropName}/{propName}/epoch/?",
                    //            Indexes = new Container<Index> { { new RangeIndex(DataType.Number, -1) } }
                    //        });
                    //    }
                    //};
                    container = await database.CreateContainerIfNotExistsAsync(containerProperties, Throughput);
                }
                catch (Exception ex)
                {
                    container = null;
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Items
        private void CheckItem(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
        }

        private PartitionKey GetPartitionKey(Object partitionKey)
        {
            PartitionKey result;
            switch (partitionKey)
            {
                case bool b:
                    result = new PartitionKey(b);
                    break;
                case double d:
                    result = new PartitionKey(d);
                    break;
                case string s:
                    result = new PartitionKey(s);
                    break;
                default:
                    //throw new ArgumentOutOfRangeException("PartionKey type can only be string, double or bool");
                    result = new PartitionKey(partitionKey.ToString());
                    break;
            }
            return result;
        }

        private PartitionKey GetPartitionKey(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            return GetPartitionKey(partitionKeyExpression.Compile()(item));
        }

        private string GetPartitionKeyValue(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null)
            {
                return null;
            }
            else
            {
                return partitionKeyExpression.Compile()(item).ToString();
            }
        }

        public async Task<T> GetItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    return await container.ReadItemAsync<T>(item.Id, GetPartitionKey(item, partitionKeyExpression));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Logger.Verbose("Item with ID {ID} not found", item.Id);
                    return default;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, CosmosDBCost<T> cost)> GetItemWithCostAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.ReadItemAsync<T>(item.Id, GetPartitionKey(item, partitionKeyExpression));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Logger.Verbose("Item with ID {ID} not found", item.Id);
                    return default;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var items = new List<T>();
                    var iterator = container.GetItemLinqQueryable<T>().Where(predicate).ToFeedIterator();
                    while (iterator.HasMoreResults)
                    {
                        foreach (var item in await iterator.ReadNextAsync())
                        {
                            items.Add(item);
                        }
                    }
                    return items;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(IEnumerable<T> items, CosmosDBCost<T> cost)> GetItemsWithCostAsync(Expression<Func<T, bool>> predicate)
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var items = new List<T>();
                    var iterator = container.GetItemLinqQueryable<T>().Where(predicate).ToFeedIterator();
                    var cost = new CosmosDBCost<T>();
                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        cost += new CosmosDBCost<T>(response);
                        foreach (var item in response)
                        {
                            items.Add(item);
                        }
                    }
                    return (items, cost);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
        #region Create
        public async Task<T> CreateItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    return await container.CreateItemAsync(item, GetPartitionKey(item, partitionKeyExpression));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, CosmosDBCost<T> cost)> CreateItemWithCostAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.CreateItemAsync<T>(item, GetPartitionKey(item, partitionKeyExpression));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<CosmosDBCost<T>> CreateItemsAsync(List<T> items, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var cost = new CosmosDBCost<T>();
                    if (!client.ClientOptions.AllowBulkExecution)
                    {
                        foreach (var item in items)
                        {
                            cost += (await CreateItemWithCostAsync(item, partitionKeyExpression)).cost;
                        }
                    }
                    else
                    {
                        var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                        foreach (var item in items)
                        {
                            tasks.Add(CreateItemWithCostAsync(item, partitionKeyExpression));
                        }
                        await Task.WhenAll(tasks);
                        if (tasks.Any(i => i.IsFaulted))
                        {
                            throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                        }
                        foreach (var task in tasks.Where(i => !i.IsFaulted))
                        {
                            cost += task.Result.cost;
                        }
                    }
                    return cost;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Replace
        public async Task<T> ReplaceItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    return await container.ReplaceItemAsync(item, item.Id, GetPartitionKey(item, partitionKeyExpression));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, CosmosDBCost<T> cost)> ReplaceItemWithCostAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.ReplaceItemAsync<T>(item, item.Id, GetPartitionKey(item, partitionKeyExpression));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<CosmosDBCost<T>> ReplaceItemsAsync(List<T> items, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var cost = new CosmosDBCost<T>();
                    if (!client.ClientOptions.AllowBulkExecution)
                    {
                        foreach (var item in items)
                        {
                            cost += (await ReplaceItemWithCostAsync(item, partitionKeyExpression)).cost;
                        }
                    }
                    else
                    {
                        var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                        foreach (var item in items)
                        {
                            tasks.Add(ReplaceItemWithCostAsync(item, partitionKeyExpression));
                        }
                        await Task.WhenAll(tasks);
                        if (tasks.Any(i => i.IsFaulted))
                        {
                            throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                        }
                        foreach (var task in tasks.Where(i => !i.IsFaulted))
                        {
                            cost += task.Result.cost;
                        }
                    }
                    return cost;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Upsert
        public async Task<T> UpsertItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    return await container.UpsertItemAsync(item, GetPartitionKey(item, partitionKeyExpression));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, CosmosDBCost<T> cost)> UpsertItemWithCostAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.UpsertItemAsync<T>(item, GetPartitionKey(item, partitionKeyExpression));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<CosmosDBCost<T>> UpsertItemsAsync(List<T> items, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var oldAutoEnsureContainer = AutoEnsureContainer;
                    try
                    {
                        AutoEnsureContainer = false;
                        var cost = new CosmosDBCost<T>();
                        if (!client.ClientOptions.AllowBulkExecution)
                        {
                            foreach (var item in items)
                            {
                                cost += (await UpsertItemWithCostAsync(item, partitionKeyExpression)).cost;
                            }
                        }
                        else
                        {
                            var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                            foreach (var item in items)
                            {
                                tasks.Add(UpsertItemWithCostAsync(item, partitionKeyExpression));
                            }
                            await Task.WhenAll(tasks);
                            if (tasks.Any(i => i.IsFaulted))
                            {
                                throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                            }
                            foreach (var task in tasks.Where(i => !i.IsFaulted))
                            {
                                cost += task.Result.cost;
                            }
                        }
                        return cost;
                    }
                    finally
                    {
                        AutoEnsureContainer = oldAutoEnsureContainer;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
        #endregion

        #region Delete
        public async Task<T> DeleteItemAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    return await container.DeleteItemAsync<T>(item.Id, GetPartitionKey(item, partitionKeyExpression));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        private async Task<(T item, CosmosDBCost<T> cost)> DeleteItemWithCostAsync(string id, Object partitionKey)
        {
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", id }, { "partitionKey", partitionKey } }))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.DeleteItemAsync<T>(id, GetPartitionKey(partitionKey));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(T item, CosmosDBCost<T> cost)> DeleteItemWithCostAsync(T item, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (partitionKeyExpression == null) throw new ArgumentNullException(nameof(partitionKeyExpression));
            using (Logger.MethodCall<T>(GetType(), new MethodCallParameters { { "id", item.Id }, { "partitionKey", GetPartitionKeyValue(item, partitionKeyExpression) } }))
            {
                try
                {
                    CheckItem(item);
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var response = await container.DeleteItemAsync<T>(item.Id, GetPartitionKey(item, partitionKeyExpression));
                    return (response, new CosmosDBCost<T>(response));
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<CosmosDBCost<T>> DeleteItemsAsync(List<T> items, Expression<Func<T, object>> partitionKeyExpression)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var oldAutoEnsureContainer = AutoEnsureContainer;
                    try
                    {
                        AutoEnsureContainer = false;
                        var cost = new CosmosDBCost<T>();
                        if (!client.ClientOptions.AllowBulkExecution)
                        {
                            foreach (var item in items)
                            {
                                cost += (await DeleteItemWithCostAsync(item, partitionKeyExpression)).cost;
                            }
                        }
                        else
                        {
                            var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                            foreach (var item in items)
                            {
                                tasks.Add(DeleteItemWithCostAsync(item, partitionKeyExpression));
                            }
                            await Task.WhenAll(tasks);
                            if (tasks.Any(i => i.IsFaulted))
                            {
                                throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                            }
                            foreach (var task in tasks.Where(i => !i.IsFaulted))
                            {
                                cost += task.Result.cost;
                            }
                        }
                        return cost;
                    }
                    finally
                    {
                        AutoEnsureContainer = oldAutoEnsureContainer;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(CosmosDBCost<T> totalCost, CosmosDBCost<T> readCost, CosmosDBCost<T> deleteCost)> DeleteItemsAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> partitionKeyExpression)
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var oldAutoEnsureContainer = AutoEnsureContainer;
                    try
                    {
                        AutoEnsureContainer = false;
                        var (items, readCost) = await GetItemsWithCostAsync(predicate);
                        var deleteCost = new CosmosDBCost<T>();
                        if (!client.ClientOptions.AllowBulkExecution)
                        {
                            foreach (var item in items)
                            {
                                deleteCost += (await DeleteItemWithCostAsync(item, partitionKeyExpression)).cost;
                            }
                        }
                        else
                        {
                            var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                            foreach (var item in items)
                            {
                                tasks.Add(DeleteItemWithCostAsync(item, partitionKeyExpression));
                            }
                            await Task.WhenAll(tasks);
                            if (tasks.Any(i => i.IsFaulted))
                            {
                                throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                            }
                            foreach (var task in tasks.Where(i => !i.IsFaulted))
                            {
                                deleteCost += task.Result.cost;
                            }
                        }
                        return (readCost + deleteCost, readCost, deleteCost);
                    }
                    finally
                    {
                        AutoEnsureContainer = oldAutoEnsureContainer;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        public async Task<(CosmosDBCost<T> totalCost, CosmosDBCost<T> readCost, CosmosDBCost<T> deleteCost)> DeleteItemsAsync(Expression<Func<T, bool>> predicate, object partitionKey)
        {
            using (Logger.MethodCall<T>(GetType()))
            {
                try
                {
                    if (AutoEnsureContainer)
                    {
                        await EnsureContainerAsync();
                    }
                    var oldAutoEnsureContainer = AutoEnsureContainer;
                    try
                    {
                        AutoEnsureContainer = false;
                        var readCost = new CosmosDBCost<T>();
                        var items = new List<string>();
                        var iterator = container.GetItemLinqQueryable<T>().Where(predicate).Select(i => i.Id).ToFeedIterator();
                        while (iterator.HasMoreResults)
                        {
                            foreach (var item in await iterator.ReadNextAsync())
                            {
                                items.Add(item);
                            }
                        }
                        var deleteCost = new CosmosDBCost<T>();
                        if (!client.ClientOptions.AllowBulkExecution)
                        {
                            foreach (var item in items)
                            {
                                deleteCost += (await DeleteItemWithCostAsync(item, partitionKey)).cost;
                            }
                        }
                        else
                        {
                            var tasks = new List<Task<(T item, CosmosDBCost<T> cost)>>();
                            foreach (var item in items)
                            {
                                tasks.Add(DeleteItemWithCostAsync(item, partitionKey));
                            }
                            await Task.WhenAll(tasks);
                            if (tasks.Any(i => i.IsFaulted))
                            {
                                throw new InvalidOperationException($"{tasks.Count(i => i.IsFaulted)} tasks faulted");
                            }
                            foreach (var task in tasks.Where(i => !i.IsFaulted))
                            {
                                deleteCost += task.Result.cost;
                            }
                        }
                        return (readCost + deleteCost, readCost, deleteCost);
                    }
                    finally
                    {
                        AutoEnsureContainer = oldAutoEnsureContainer;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }
            }
        }

        #endregion

        #endregion
    }

}
