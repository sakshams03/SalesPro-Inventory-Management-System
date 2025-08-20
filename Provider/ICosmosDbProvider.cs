using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Provider
{
    public interface ICosmosDbProvider
    {
        Task AddAsync<T>(T item, string containerId, string partitionKey = null);
        Task DeleteAsync<T>(string id, string containerId, string partitionKey = null);
        Task<List<T>> ListAsync<T>(QueryDefinition query, string containerId, string partitionKey = null);
        Task UpdateAsync<T>(string id, string partitionKey, Action<T> updateAction, string containerId);


    }
}
