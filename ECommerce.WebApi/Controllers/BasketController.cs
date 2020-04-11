using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using UserActor.Interfaces;

namespace ECommerce.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        [HttpGet("{userId}")]
        public async Task<ApiBasket> GetAsync(string userId) 
        {
            var actor = GetActor(userId);

            var products = await actor.GetBasketAsync();

            return new ApiBasket 
            {
                Items = products.Select(p => new ApiBasketItem { ProductId = p.ProductId.ToString(), Quantity = p.Quantity }).ToArray(),
                UserId = userId
            };
        }

        [HttpPost("{userId}")]
        public async Task AddAsync(string userId, [FromBody] ApiBasketAddRequest request) 
        {
            var actor = GetActor(userId);

            await actor.AddToBasketAsync(request.ProductId, request.Quantity);
        }

        [HttpDelete("{userId}")]
        public async Task DeleteAsync(string userId) 
        {
            var actor = GetActor(userId);

            await actor.ClearBasketAsync();
        }

        private IUserActor GetActor(string userId) 
        {
            return ActorProxy.Create<IUserActor>(new ActorId(userId), new Uri("fabric:/ECommerce/UserActorService"));
        }
    }
}