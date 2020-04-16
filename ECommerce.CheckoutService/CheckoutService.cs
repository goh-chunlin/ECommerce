using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.CheckoutService.Model;
using ECommerce.ProductCatalog.Model;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using UserActor.Interfaces;

namespace ECommerce.CheckoutService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CheckoutService : StatefulService, ICheckoutService
    {
        public CheckoutService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<CheckoutSummary> CheckoutAsync(string userId)
        {
            var checkoutSummary = new CheckoutSummary();

            checkoutSummary.Date = DateTime.UtcNow;
            checkoutSummary.Products = new List<CheckoutProduct>();

            IUserActor userActor = GetUserActor(userId);
            var userBasketItems = await userActor.GetBasketAsync();
            var basket = new Dictionary<Guid, int>();
            foreach (var userBasketItem in userBasketItems) 
            {
                basket.Add(userBasketItem.ProductId, userBasketItem.Quantity);
            }

            IProductCatalogService catalogService = GetProductCatalogService();

            foreach (KeyValuePair<Guid, int> basketLine in basket) 
            {
                Product product = await catalogService.GetProductAsync(basketLine.Key);

                var checkoutProduct = new CheckoutProduct
                {
                    Product = product,
                    Quantity = basketLine.Value,
                    SubTotal = product.Price * basketLine.Value
                };

                checkoutSummary.Products.Add(checkoutProduct);

                checkoutSummary.TotalPrice += checkoutProduct.SubTotal;
            }

            await AddToHistoryAsync(checkoutSummary);

            return checkoutSummary;
        }

        public async Task<CheckoutSummary[]> GetOrderHistoryAsync(string userId)
        {
            var checkoutSummaryRecords = new List<CheckoutSummary>();

            IReliableDictionary<DateTime, CheckoutSummary> history =
                await StateManager.GetOrAddAsync<IReliableDictionary<DateTime, CheckoutSummary>>("history");

            using (ITransaction transaction = StateManager.CreateTransaction()) 
            {
                Microsoft.ServiceFabric.Data.IAsyncEnumerable<KeyValuePair<DateTime, CheckoutSummary>> allProducts =
                    await history.CreateEnumerableAsync(transaction, EnumerationMode.Unordered);

                using (Microsoft.ServiceFabric.Data.IAsyncEnumerator<KeyValuePair<DateTime, CheckoutSummary>> enumerator = allProducts.GetAsyncEnumerator()) 
                {
                    while (await enumerator.MoveNextAsync(CancellationToken.None)) 
                    {
                        checkoutSummaryRecords.Add(enumerator.Current.Value);
                    }
                }
            }

            return checkoutSummaryRecords.ToArray();
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
                new ServiceReplicaListener(context => new FabricTransportServiceRemotingListener(context, this))
            };
        }

        private IUserActor GetUserActor(string userId) 
        {
            return ActorProxy.Create<IUserActor>(
                new ActorId(userId),
                new Uri("fabric:/ECommerce/UserActorService"));
        }

        private IProductCatalogService GetProductCatalogService() 
        {
            var proxyFactory = new ServiceProxyFactory(c => new FabricTransportServiceRemotingClientFactory());

            return proxyFactory.CreateNonIServiceProxy<IProductCatalogService>(
                new Uri("fabric:/ECommerce/ECommerce.ProductCatalog"),
                new ServicePartitionKey(0));
        }

        private async Task AddToHistoryAsync(CheckoutSummary checkout) 
        {
            IReliableDictionary<DateTime, CheckoutSummary> history = await StateManager.GetOrAddAsync<IReliableDictionary<DateTime, CheckoutSummary>>("history");

            using (ITransaction transaction = StateManager.CreateTransaction())
            {
                await history.AddAsync(transaction, checkout.Date, checkout);

                await transaction.CommitAsync();
            }
        }
    }
}
