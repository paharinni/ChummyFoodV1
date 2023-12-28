using Microsoft.AspNetCore.Http;

namespace ChummyFoodBack.Feature.RetailManagement.Products;

public class ProductCreateModel
{
    public int CategoryId { get; set; }

    public string Name { get; set; }

    public double Price { get; set; }

    public string? Currency { get; set; }

    public string? ProductDownText { get; set; }

    public string ProductDescription { get; set; }

    public int AvailableCount { get; set; }

    public string MailFileContent { get; set; }

    public IFormFile ProductImage { get; set; }
}
