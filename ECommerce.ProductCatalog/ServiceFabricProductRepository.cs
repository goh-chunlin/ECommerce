using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.ProductCatalog.Model;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace ECommerce.ProductCatalog
{
    class ServiceFabricProductRepository : IProductRepository
    {
        private readonly IReliableStateManager _stateManager;

        public ServiceFabricProductRepository(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public async Task AddProductAsync(Product newProduct)
        {
            var products = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, Product>>("products");

            using (var transaction = _stateManager.CreateTransaction()) 
            {
                await products.AddOrUpdateAsync(transaction, newProduct.Id, newProduct, (id, value) => newProduct);

                await transaction.CommitAsync();
            }

        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var output = new List<Product>();

            var products = await _stateManager.GetOrAddAsync<IReliableDictionary<Guid, Product>>("products");

            using (var transaction = _stateManager.CreateTransaction())
            {
                var allProducts = await products.CreateEnumerableAsync(transaction, EnumerationMode.Ordered);

                using var enumerator = allProducts.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    output.Add(enumerator.Current.Value);
                }
            }


            return output;
        }
    }
}
