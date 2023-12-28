namespace ChummyFoodBack.Feature.RetailManagement.Products
{
    public class ProductModel
    {
        public string PhotoUrl { get; set; }

        public int CategoryId { get; set; }

        public int Id { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public string Currency { get; set; }

        public string? ProductDownText { get; set; }

        public string ProductDescription { get; set; }

        public int AvailableCount { get; set; }
    }
}
