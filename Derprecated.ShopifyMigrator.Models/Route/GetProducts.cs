using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Derprecated.ShopifyMigrator.Models.Route
{
    [Route("/admin/products.json", "GET")]
    public class GetProducts : IReturn<GetProductsResponse>
    {
        [Alias("limit")]
        public int Limit { get; set; }
    }
}