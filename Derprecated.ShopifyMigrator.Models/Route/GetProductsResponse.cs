using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace Derprecated.ShopifyMigrator.Models.Route
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GetProductsResponse
    {
        [Alias("products")]
        public List<Dto.Product> Products { get; set; }
    }
}