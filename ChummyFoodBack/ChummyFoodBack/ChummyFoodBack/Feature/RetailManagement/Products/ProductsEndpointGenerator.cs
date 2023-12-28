using ChummyFoodBack.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChummyFoodBack.Feature.RetailManagement.Products;

public class ProductsEndpointGenerator
{
    private readonly string _endpointUrlBasePath;

    public ProductsEndpointGenerator(IOptions<ImageUrlOptions> options)
    {
        _endpointUrlBasePath = options.Value.ImageUrlBasePath;
    }

    public string CreatePhotoUrlFromProductId(IUrlHelper urlHelper, int productId)
    {
        string imageAction = _endpointUrlBasePath + urlHelper.Action("GetImage", "Image", new { productId = productId })!;
        return imageAction;
    }
}
