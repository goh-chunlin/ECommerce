using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Threading.Tasks;

namespace ECommerce.CheckoutService.Model
{
    public interface ICheckoutService : IService
    {
        Task<CheckoutSummary> CheckoutAsync(string userId);

        Task<CheckoutSummary[]> GetOrderHistoryAsync(string userId);
    }
}
