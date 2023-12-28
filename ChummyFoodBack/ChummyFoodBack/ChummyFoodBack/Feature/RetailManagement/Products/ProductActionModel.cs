using System.Collections.Generic;

namespace ChummyFoodBack.Feature.RetailManagement.Products
{
    public class ProductActionModel
    {
        public int CategoryId { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public string Currency { get; set; }

        public int AvailableCount { get; set; }

        public IEnumerable<string> MailFileContent { get; set; }
    }
}
