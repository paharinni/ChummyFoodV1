using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.RetailManagement.Categories;


[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly CommerceContext _commerceContext;
    public CategoriesController(CommerceContext commerceContext)
    {
        _commerceContext = commerceContext;
    }
    [HttpDelete("{categoryId:int:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromRoute] int categoryId)
    {
        CategoryDAO? categoryDao = await _commerceContext.Categories
            .SingleOrDefaultAsync(category => category.Id == categoryId);
        if (categoryDao is not null)
        {
            _commerceContext.Remove(categoryDao);
            await _commerceContext.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CategoryCreateModel categoryModel)
    {
        CategoryDAO? categoryDao = await _commerceContext.Categories
            .SingleOrDefaultAsync(category => category.Name == categoryModel.Name);
        if (categoryDao is not null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Category already exists" }
            });
        }

        await _commerceContext.AddAsync(new CategoryDAO
        {
            Name = categoryModel.Name
        });
        await _commerceContext.SaveChangesAsync();
        return Ok();
    }


    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(CategoryModel categoryModel)
    {
        CategoryDAO? category = await _commerceContext.Categories
            .SingleOrDefaultAsync(category => category.Id == categoryModel.Id);
        if (category is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Category not exists" }
            });
        }
        category.Name = categoryModel.Name;
        _commerceContext.Update(category);
        await _commerceContext.SaveChangesAsync();
        return Ok();
    }


    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<CategoryModel>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var categoryModels = await _commerceContext.Categories.Select(categoryDao => new CategoryModel
        {
            Id = categoryDao.Id,
            Name = categoryDao.Name
        }).ToListAsync();
        return Ok(categoryModels);
    }
}
