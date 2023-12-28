using System.Security.Claims;
using ChummyFoodBack.Feature.Notification;
using ChummyFoodBack.Options;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChummyFoodBack.Feature.IdentityManagement;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class IdentityManagementController: ControllerBase
{
    private readonly CommerceContext _context;
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly IIdentityService _identityService;
    private readonly IUserNotificationService _notificationService;

    public IdentityManagementController(
        CommerceContext context,
        IOptions<SecurityOptions> securityOptions,
        IIdentityService identityService,
        IUserNotificationService userNotificationService)
    {
        _context = context;
        _securityOptions = securityOptions;
        _identityService = identityService;
        _notificationService = userNotificationService;
    }

    [HttpGet("userInfo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateIdentityDTO>> GetUserInfo(string email)
    {
        if (email.Length == 0)
        {
            return BadRequest("Invalid email");
        }

        var user = (await _identityService.GetUsers()).FirstOrDefault(u => u.Email.Equals(email));

        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UpdateIdentityDTO
        {
            Email = user.Email,
            Age = user.Age,
            City = user.City,
            Name = user.Name,
            Country = user.Country,
            Surname = user.Surname
        });
    }
    
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(IdentityModel model)
    {
        var isEntityExists = await _context
            .Identities
            .AnyAsync(identity => identity.Email == model.Email);
        if (isEntityExists)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "Email already exists" }
            });
        }

        await _identityService.RegisterUser(model, "User");
        await _notificationService.NotifyUserRegistered(new UserModel
        {
            UserEmail = model.Email,
            UserPassword = model.Password
        });
        
        return Ok();
    }

    [HttpPatch("update")]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UpdateUser(UpdateIdentityDTO user)
    {
        var isUserExists = await _identityService.CheckUserExistence(user.Email);

        if (!isUserExists)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "User does not exist" }
            });
        }
        
        var newUser = await _identityService.UpdateUserAsync(user);

        return Ok();
    }
    

    [HttpPost("validateRestoreCode")]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyRestoreCode(ValidateRestoreCodeModel model)
    {
        var validateResult = await _identityService.ValidateRestoreCode(model);
        if (validateResult.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(new ErrorModel
        {
            Errors = new[]{validateResult.Error!}
        });
    }
    

    [HttpPost("tryStartPasswordRestore")]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TryPasswordCheck(TryPasswordCheckModel passwordCheckModel)
    {
        var restoreResult = await _identityService.StartPasswordRestoreForEmail(passwordCheckModel);
        if (restoreResult.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(new ErrorModel
        {
            Errors = new[]{restoreResult.Error!.ErrorMessage}
        });
    }

    [HttpPost("restorePassword")]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RestorePassword(ChangePasswordModel changePasswordModel)
    {
        var changePasswordResult = await _identityService.RestorePassword(changePasswordModel);
        if (!changePasswordResult.IsSuccess)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[]{changePasswordResult.Error!}
            });
        }

        return Ok();
    }
    
    
    [HttpGet("roles")]
    [Authorize]
    [ProducesResponseType(typeof(RolesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetRoles()
    {
        var userRole = this.HttpContext.User.Claims.First(claim => claim.Type is ClaimTypes.Role).Value;
        return Ok(new RolesResponse
        {
            Role = userRole
        });
    }
    
    
    
    [HttpPost("authenticate")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetToken(IdentityModel model)
    {
        var targetIdentity = 
            await _context.Identities.Include(identity => identity.RoleDao).
                FirstOrDefaultAsync(dao => dao.Email == model.Email);
        if (targetIdentity is null)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[] { "User with that email does not exists" }
            });
        }

        if (_identityService.CheckIdentitiesEqual(model, targetIdentity))
        {
            string token = TokenBuilder.FromIdentity(targetIdentity, _securityOptions.Value.SecretKey);
            return Ok(new AuthenticationResponse
            {
                Email = model.Email,
                Token = token
            });
        }
        
        return BadRequest(new ErrorModel
            {
                Errors = new[] { "Invalid password" }
            });
        
    }
}
