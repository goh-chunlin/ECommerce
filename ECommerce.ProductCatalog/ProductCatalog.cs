using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.ProductCatalog.Model;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ECommerce.ProductCatalog
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ProductCatalog : StatefulService, IProductCatalogService
    {
        private IProductRepository _repoProduct;

        public ProductCatalog(StatefulServiceContext context)
            : base(context)
        { }

        public async Task AddProductAsync(Product newProduct)
        {
            await _repoProduct.AddProductAsync(newProduct);
        }

        public async Task<Product[]> GetAllProductsAsync()
        {
            return (await _repoProduct.GetAllProductsAsync()).ToArray();
        }

        public async Task<Product> GetProductAsync(Guid productId) 
        {
            return (await _repoProduct.GetAllProductsAsync()).FirstOrDefault(p => p.Id == productId);
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] 
            {
                new ServiceReplicaListener(context =>
                    new FabricTransportServiceRemotingListener(context, this))
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _repoProduct = new ServiceFabricProductRepository(StateManager);

            var product1 = new Product { Id = Guid.NewGuid(), Name = "Product 1", Description = "Description 1", Price = 1.02m, Availability = 10 };
            var product2 = new Product { Id = Guid.NewGuid(), Name = "Product 2", Description = "Description 2", Price = 1.03m, Availability = 20 };
            var product3 = new Product { Id = Guid.NewGuid(), Name = "Product 3", Description = "Description 3", Price = 1.04m, Availability = 30 };
            var product4 = new Product { Id = Guid.NewGuid(), Name = "Product 4", Description = "Description 4", Price = 1.05m, Availability = 40 };
            var product5 = new Product { Id = Guid.NewGuid(), Name = "Product 5", Description = "Description 5", Price = 1.06m, Availability = 50 };

            await _repoProduct.AddProductAsync(product1);
            await _repoProduct.AddProductAsync(product2);
            await _repoProduct.AddProductAsync(product3);
            await _repoProduct.AddProductAsync(product4);
            await _repoProduct.AddProductAsync(product5);

            var allProducts = await _repoProduct.GetAllProductsAsync();
        }
    }
}
