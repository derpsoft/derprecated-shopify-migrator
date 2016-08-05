using ServiceStack;

namespace Derprecated.ShopifyMigrator.Models.Route
{
    [Route("/admin/products/count.json", "GET")]
    public class GetProductsCount : IReturn<GetProductsCountResponse>
    {
    }
}