using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Shared;
using ChummyFoodBack.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChummyFoodBack.Feature.RetailManagement.Products;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ImageFileManagement _imageFileManagement;
    private readonly CommerceContext _commerceContext;
    private readonly ILogger<ProductController> _logger;
    private readonly ProductsEndpointGenerator _endpointGenerator;
    private const int AllItemsCategory = -1;

    public ProductController(
        ImageFileManagement imageFileManagement,
        CommerceContext commerceContext,
        ILogger<ProductController> logger,
        ProductsEndpointGenerator endpointGenerator)
    {
        _imageFileManagement = imageFileManagement;
        this._commerceContext = commerceContext;
        _logger = logger;
        _endpointGenerator = endpointGenerator;
    }

    private ProductModel MapProduct(ProductDAO product)
    {
        return new ProductModel
        {
            Id = product.Id,
            Currency = this.GetUnitOfPrice(product),
            Name = product.Name,
            Price = product.Price,
            AvailableCount = product.ProductCostItems.Count(costItem => costItem.OwnedBy is null),
            CategoryId = product.CategoryId,
            PhotoUrl = _endpointGenerator.CreatePhotoUrlFromProductId(Url, product.Id),
            ProductDescription = product.ProductDescription,
            ProductDownText = product.ProductDownText
        };
    }

    [HttpGet("category/{categoryId:int:required}")]
    [ProducesResponseType(typeof(IEnumerable<ProductModel>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        if (categoryId == AllItemsCategory)
        {
            var products = await _commerceContext.Products
                .Include(product => product.ProductCostItems)
                .ToListAsync();
            var responseProducts = products.Select(MapProduct);
            return Ok(responseProducts);
        }
        var persistedCategory = await _commerceContext.Categories
            .Include(cat => cat.Products)
            .ThenInclude(product => product.ProductCostItems)
            .SingleOrDefaultAsync(cat => cat.Id == categoryId);

        if (persistedCategory is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Requested category doesn't exists" }
            });
        }
        var responseProductsForCategoryId = persistedCategory.Products.Select(MapProduct);
        return Ok(responseProductsForCategoryId);
    }

    [HttpGet("{productId:int:required}")]
    [ProducesResponseType(typeof(ProductModel),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var requestedProduct = await _commerceContext
            .Products
            .Include(products => products.ProductCostItems)
            .FirstOrDefaultAsync(product => product.Id == productId);
        if (requestedProduct is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Product not exists with that id" }
            });
        }
        return Ok(MapProduct(requestedProduct));
    }

    [HttpPost("range")]
    [ProducesResponseType(typeof(IEnumerable<ProductModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GerByProductIds(RangeProductRequest productsIds)
    {
        var requestedIdsSet = new HashSet<int>(productsIds.Ids);
        var products =
            await _commerceContext.Products
                .Include(products => products.ProductCostItems)
                .Where(product => requestedIdsSet.Contains(product.Id))
                .ToListAsync();
        return Ok(products.Select(MapProduct));
    }


    [HttpPost]
    [ProducesResponseType(typeof(ErrorModel),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductCreateModel productModel)
    {
        List<string> errors = new List<string>();
        var isCategoryExists = await _commerceContext.Categories.AnyAsync(category => category.Id == productModel.CategoryId);
        if (!isCategoryExists)
        {
            errors.Add("Requested category does not exists");
        }
        bool isProductWithNameExists = await _commerceContext.Products.AnyAsync(product => product.Name == productModel.Name);
        if (isProductWithNameExists)
        {
            errors.Add("Product with that name already exists");
        }

        if (isProductWithNameExists || !isCategoryExists)
        {
            return BadRequest(new ErrorModel
            {
                Errors = errors
            });
        }

        string fileName = await PersistFile(productModel.ProductImage);
        var productContents = JsonSerializer.Deserialize<string[]>(productModel.MailFileContent)!;
        var dao = new ProductDAO
        {
            ImageContentType = productModel.ProductImage.ContentType,
            CategoryId = productModel.CategoryId,
            Name = productModel.Name,
            ProductDescription = productModel.ProductDescription,
            Price = productModel.Price,
            UnitOfPrice = productModel.Currency,
            ImageFileName = fileName,
            ProductDownText = productModel.ProductDownText,
            ProductCostItems = productContents.Select(item => new ProductCostItemsDAO
            {
                UserUnderstandableItem = item
            }).ToList()
        };
        await _commerceContext.Products.AddAsync(dao);
        await _commerceContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{productId:int:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        ProductDAO? productToDelete = await _commerceContext
                                            .Products
                                            .FirstOrDefaultAsync(product => product.Id == productId);
        if (productToDelete is not null)
        {
            _commerceContext.Products.Remove(productToDelete);
            await _commerceContext.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpGet("{productId:int:required}/WithUserMessage")]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserMessageExtendedProductModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductWithUserMessage(int productId)
    {
        ProductDAO? productDao = await _commerceContext.Products
            .Include(product => product.ProductCostItems)
            .FirstOrDefaultAsync(item => item.Id == productId);
        if (productDao is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Product not exists with that id" }
            });
        }

        //send to ui only not payed items
        return Ok(new UserMessageExtendedProductModel
        {
            Id = productDao.Id,
            Currency = this.GetUnitOfPrice(productDao),
            Name = productDao.Name,
            Price = productDao.Price,
            CategoryId = productDao.CategoryId,
            PhotoUrl = _endpointGenerator.CreatePhotoUrlFromProductId(Url, productDao.Id),
            ProductDescription = productDao.ProductDescription,
            ProductDownText = productDao.ProductDownText,
            AvailableCount = productDao.ProductCostItems.Count(costItem => costItem.OwnedBy is null),
            UserMessages = productDao.ProductCostItems
                .Where(costItem => costItem.OwnedBy is null)
                .Select(costItem => costItem.UserUnderstandableItem)
        });
    }


    [HttpPatch]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProduct([FromForm] ProductUpdateModel productModel)
    {
        ProductDAO? productToUpdate = await _commerceContext.Products.FirstOrDefaultAsync(product => product.Id == productModel.Id);
        if (productToUpdate is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Product that you gonna update not exists" }
            });
        }

        string imageFileName = productToUpdate.ImageFileName;
        if (productModel.ProductImage is not null)
        {
            try
            {
                System.IO.File.Delete(_imageFileManagement.GenerateFilePath(imageFileName));
            }
            catch (Exception ex)
            {
                //File already deleted, maybe concurrency error 
                _logger.LogError("Exception of file deletion maybe caused by lock or concurrency error");
                _logger.LogError(ex.Message, ex.StackTrace);
                return BadRequest(new ErrorModel
                {
                    Errors = new[] { "Some unknown update error, please try again" }
                });
            }

            string newName = await PersistFile(productModel.ProductImage);
            imageFileName = newName;
            productToUpdate.ImageContentType = productModel.ProductImage.ContentType;
        }

        var productContents = JsonSerializer.Deserialize<string[]>(productModel.MailFileContent)!;
        //TODO change to upsert but relational db is so fast for that type of workloads
        var previousCostItems =
            await _commerceContext.ProductCostItems
                .Where(costItem => costItem.ProductId == productToUpdate.Id
                                   && costItem.OwnedBy == null)
                .ToListAsync();
        _commerceContext.RemoveRange(previousCostItems);


        productToUpdate.CategoryId = productModel.CategoryId;
        productToUpdate.Name = productModel.Name;
        productToUpdate.ProductDescription = productModel.ProductDescription;
        productToUpdate.Price = productModel.Price;
        productToUpdate.UnitOfPrice = productModel.Currency;
        productToUpdate.ImageFileName = imageFileName;
        productToUpdate.ProductDownText = productModel.ProductDownText;
        productToUpdate.ProductCostItems = productContents.Select(item => new ProductCostItemsDAO
        {
            UserUnderstandableItem = item
        }).ToList();

        _commerceContext.Products.Update(productToUpdate);
        await _commerceContext.SaveChangesAsync();
        return Ok();
    }


    private async Task<string> PersistFile(IFormFile file)
    {
        await using Stream inputImageStream = file.OpenReadStream();
        var fileData = await _imageFileManagement.ReadImageStream(inputImageStream);
        string fileExtension = file.FileName.Split(".")[^1];
        Guid fileGuid = Guid.NewGuid();
        string fileName = fileGuid + "." + fileExtension;
        string filePath = _imageFileManagement.GenerateFilePath(fileName);
        await using var sw = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await sw.WriteAsync(fileData, 0, fileData.Length);
        return fileName;
    }

    private string GetUnitOfPrice(ProductDAO persistedProduct)
        => persistedProduct.UnitOfPrice switch
        {
            null => string.Empty,
            var unitOfPrice => unitOfPrice
        };
}
