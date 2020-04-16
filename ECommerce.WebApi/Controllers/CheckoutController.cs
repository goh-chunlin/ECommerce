using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.CheckoutService.Model;
using ECommerce.WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace ECommerce.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private static readonly Random random = new Random(DateTime.UtcNow.Second);

        [Route("{userId}")]
        public async Task<ApiCheckoutSummary> CheckoutAsync(string userId) 
        {
            var checkoutSummary = await GetCheckoutService().CheckoutAsync(userId);

            return ToApiCheckoutSummary(checkoutSummary);
        }

        [Route("history/{userId}")]
        public async Task<IEnumerable<ApiCheckoutSummary>> GetHistoryAsync(string userId)
        {
            var checkoutHistoryRecords = await GetCheckoutService().GetOrderHistoryAsync(userId);

            return checkoutHistoryRecords.Select(ToApiCheckoutSummary);
        }


        private ICheckoutService GetCheckoutService() 
        {
            long key = LongRandom();

            var proxyFactory = new ServiceProxyFactory(
                c => new FabricTransportServiceRemotingClientFactory());

            return proxyFactory.CreateServiceProxy<ICheckoutService>(
                new Uri("fabric:/ECommerce/ECommerce.CheckoutService"),
                new ServicePartitionKey(key));
        }

        private ApiCheckoutSummary ToApiCheckoutSummary(CheckoutSummary model) 
        {
            return new ApiCheckoutSummary 
            {
                Products = model.Products.Select(p => new ApiCheckoutProduct 
                {
                    ProductId = p.Product.Id,
                    ProductName = p.Product.Name,
                    Price = p.SubTotal,
                    Quantity = p.Quantity
                }).ToList(),
                TotalPrice = model.TotalPrice,
                Date = model.Date
            };
        }

        private long LongRandom() 
        {
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);
            return Math.Abs(longRand % long.MaxValue);
        }
    }
}