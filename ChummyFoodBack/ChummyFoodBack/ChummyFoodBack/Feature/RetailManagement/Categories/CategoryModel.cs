using System.ComponentModel.DataAnnotations;

namespace ChummyFoodBack.Feature.RetailManagement.Categories;

public class CategoryModel
{
    public int? Id { get; set; }

    [Required]
    public string Name { get; set; }
}
