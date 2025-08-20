using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Provider
{
    public class CosmosDbProvider : ICosmosDbProvider
    {
        private readonly Database _database;
        private readonly IConfiguration _configuration;
        public CosmosDbProvider(CosmosClient client, string database, IConfiguration configuration )
        {
            _database = client.GetDatabase(database);
            _configuration = configuration;
        }
        public async Task AddAsync<T>(T item, string containerId, string partitionKey = null)
        {
            var container = _database.GetContainer(containerId);
            if (string.IsNullOrEmpty(partitionKey))
            {
                await container.CreateItemAsync(item);
            }
            else await container.CreateItemAsync(item, new PartitionKey(partitionKey));
        }

        public async Task UpdateAsync<T>( string id, string partitionKey, Action<T> updateAction, string containerId)
        {
            var container = _database.GetContainer(containerId);
            int attempt = 0;
            var maxRetries = int.Parse(_configuration["MaxRetries"] ?? "3");
            var delayMilliseconds = int.Parse(_configuration["DelayMilliseconds"] ?? "200");

            while (true)
            {
                attempt++;

                var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                var item = response.Resource;
                var etag = response.ETag;
                updateAction(item);

                try
                {
                    await container.ReplaceItemAsync(
                        item,
                        id,
                        new PartitionKey(partitionKey),
                        new ItemRequestOptions { IfMatchEtag = etag }
                    );

                    return;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    if (attempt >= maxRetries)
                    {
                        throw new InvalidOperationException(
                            $"Failed to update item {id} after {maxRetries} retries due to concurrent modifications.", ex);
                    }

                    await Task.Delay(delayMilliseconds * attempt* attempt);
                }
            }
        }

        public async Task DeleteAsync<T>(string id, string containerId, string partitionKey)
        {
            var container = _database.GetContainer(containerId);
            await container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
        }

        public async Task<List<T>> ListAsync<T>(QueryDefinition query, string containerId, string partitionKey = null)
        {
            var container = _database.GetContainer(containerId);
            var queryRequestOptions = new QueryRequestOptions
            {
                MaxConcurrency = -1,
                MaxItemCount = 1,
            };
            var iterator = container.GetItemQueryIterator<T>(query, requestOptions: queryRequestOptions);
            var list = new List<T>();
            while(iterator.HasMoreResults)
            {
                var items = await iterator.ReadNextAsync();
                foreach( var item in  items)
                {
                    list.Add(item);
                }
                
            }
            return list;

        }

        /* this api will be exposed in case list is heavy and paginated display needs to be made*/
        //public async Task<(List<T> Items, string ContinuationToken)> GetByQueryAsync<T>(QueryDefinition query, string containerId, string continuationToken = null, int pageSize = 10)
        //{
        //    var container = _database.GetContainer(containerId);

        //    var queryRequestOptions = new QueryRequestOptions
        //    {
        //        MaxItemCount = pageSize
        //    };

        //    var iterator = container.GetItemQueryIterator<T>(
        //        query,
        //        continuationToken,
        //        queryRequestOptions
        //    );

        //    if (iterator.HasMoreResults)
        //    {
        //        var response = await iterator.ReadNextAsync();
        //        return (response.ToList(), response.ContinuationToken);
        //    }

        //    return (new List<T>(), null);
        //}


    }
}
