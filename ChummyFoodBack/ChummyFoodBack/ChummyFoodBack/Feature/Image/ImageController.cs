using System;
using System.Threading.Tasks;
using ChummyFoodBack.Files;
using ChummyFoodBack.Persistance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChummyFoodBack.Feature.Image;

[ApiController]
[Route("api/[controller]")]
public class ImageController: ControllerBase
{
    private readonly CommerceContext _commerceContext;
    private readonly ImageFileManagement _fileManagement;
    private readonly ILogger<ImageController> _imageLogger;
    
    public ImageController(
        ImageFileManagement fileManagement,
        ILogger<ImageController> imageLogger, CommerceContext commerceContext)
    {
        _imageLogger = imageLogger;
        _commerceContext = commerceContext;
        _fileManagement = fileManagement;
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImage(int productId)
    {
        var targetProduct = 
            await _commerceContext.Products.FirstOrDefaultAsync(product => product.Id == productId);

        if (targetProduct is null)
        {
            return BadRequest("Image not exists");
        }

        string targetFilePath = _fileManagement.GenerateFilePath(targetProduct.ImageFileName);
        try
        {
            await using System.IO.FileStream fileStream = System.IO.File.OpenRead(targetFilePath);
            string contentType = targetProduct.ImageContentType;
            byte[] imageData = await _fileManagement.ReadImageStream(fileStream);
            return new FileContentResult(imageData, contentType);
        }
        catch (Exception ex)
        {
            return BadRequest("Internal retrieval error. Retry");
        }
    }
}
