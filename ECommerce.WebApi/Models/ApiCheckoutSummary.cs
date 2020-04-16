using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.WebApi.Models
{
    public class ApiCheckoutSummary
    {
        [JsonProperty("products")]
        public List<ApiCheckoutProduct> Products { get; set; }

        [JsonProperty("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
