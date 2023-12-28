using ChummyFoodBack.Feature.Notification;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;
using ChummyFoodBack.Utilis;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.IdentityManagement;

public class IdentityService: IIdentityService
{
    private readonly CommerceContext _context;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IPasswordGenerator _passwordGenerator;
    private const int RestoreCodeSecondsValid = 60 * 30;
    public IdentityService(
        CommerceContext context, 
        IUserNotificationService userNotificationService,
        IPasswordGenerator passwordGenerator)
    {
        _context = context;
        _userNotificationService = userNotificationService;
        _passwordGenerator = passwordGenerator;
    }
    public async Task RegisterUser(IdentityModel model, string targetRole)
    {
        var userRoleEntity = await _context.Roles.FirstOrDefaultAsync(role => role.Name == targetRole);
        var hashingResult = PasswordHasher.HashPassword(model.Password);
        var accountDao = new IdentityDAO
        {
            Email = model.Email,
            PasswordHash = hashingResult.PasswordHash,
            PasswordSalt = hashingResult.PasswordSalt,
            RoleDao = userRoleEntity ?? new RoleDao
            {
                Name = "User"
            },
            Age = 21,
            City = "test",
            Country = "test",
            Name = "Test",
            Surname = "Test"
        };
        await this._context.Identities.AddAsync(accountDao);
        await this._context.SaveChangesAsync();
    }

    public async Task<OperationValuedResult<IdentityGeneratedCreadentials, string>> RegisterUserWithoutPassword(string email)
    {
        bool isEmailAlreadyExists = await _context.Identities
            .AnyAsync(identity => identity.Email == email);
        if (isEmailAlreadyExists)
        {
            return new()
            {
                IsSuccess = false,
                Error = $"User with email: {email} already exists"
            };
        }

        string generatedPassword = _passwordGenerator.GeneratePassword(10, 15);
        var identityModel = new IdentityModel
        {
            Email = email,
            Password = generatedPassword
        };

        await this.RegisterUser(identityModel, "User");

        await this._userNotificationService.NotifyUserCreated(new UserModel
        {
            UserEmail = email,
            UserPassword = generatedPassword
        });
        
        return new()
        {
            IsSuccess = true,
            Result = new IdentityGeneratedCreadentials
            {
                Password = generatedPassword
            }
        };
    }
    

    public bool CheckIdentitiesEqual(IdentityModel first, IdentityDAO second)
    {
        return first.Email == second.Email 
               && PasswordHasher.VerifyPassword(first.Password, second.PasswordHash, second.PasswordSalt);
    }

    public async Task<OperationResult<string>> ValidateRestoreCode(ValidateRestoreCodeModel model)
    {
        IdentityDAO? targetIdentity = await this._context.Identities
            .SingleOrDefaultAsync(identity => identity.Email == model.Email);
        if (targetIdentity is null)
        {
            return new()
            {
                IsSuccess = false,
                Error = "Request Email not exists"
            };
        }

        var targetRestoreCode =
            await _context.RestoreCodes
                .Where(restoreCode => restoreCode.IdentityId == targetIdentity.Id)
                .FirstOrDefaultAsync(code => code.RestoreCode == model.RestoreCode);
        if (targetRestoreCode is null)
        {
            return new()
            {
                IsSuccess = false,
                Error = "Requested restore code not exists",
            };
        }

        if (DateTimeOffset.UtcNow > targetRestoreCode.Valid)
        {
            return new()
            {
                IsSuccess = false,
                Error = "Restore code outdated"
            };
        }

        bool isNewRestoreCodesIssuedAfterCurrent = await _context.RestoreCodes.Where(restoreCode =>
            restoreCode.IdentityId == targetIdentity.Id 
            && restoreCode.Issued > targetRestoreCode.Issued)
                .AnyAsync();

        if (isNewRestoreCodesIssuedAfterCurrent)
        {
            return new()
            {
                IsSuccess = false,
                Error = "New restore codes was issued after that"
            };
        }

        return new()
        {
            IsSuccess = true
        };
    }

    public async Task<OperationResult<StartPasswordRestoreErrorModel>> StartPasswordRestoreForEmail(TryPasswordCheckModel passwordCheckModel)
    {
        var targetIdentity = await _context.Identities
            .FirstOrDefaultAsync(identity => identity.Email == passwordCheckModel.Email);
        if (targetIdentity is null)
        {
            return new()
            {
                IsSuccess = false,
                Error = new StartPasswordRestoreErrorModel
                {
                    ErrorMessage = "Unable to find user with requested email"
                }
            };
        }

        var restoreCode = this.GenerateRestoreCode();
        
        await _context.RestoreCodes.AddAsync(new RestoreCodeDAO
        {
            RestoreCode = restoreCode,
            Issued = DateTimeOffset.UtcNow,
            Valid = DateTimeOffset.UtcNow.AddSeconds(RestoreCodeSecondsValid),
            Identity = targetIdentity
        });

        await this._userNotificationService.NotifyRestorePassword(new RestoreNotificationModel
        {
            Email = targetIdentity.Email,
            RestoreCode = restoreCode
        });
        
        await _context.SaveChangesAsync();
        return new OperationResult<StartPasswordRestoreErrorModel>()
        {
            IsSuccess = true
        };
    }

    public async Task<OperationResult<string>> RestorePassword(ChangePasswordModel model)
    {
        var validationResult = await this.ValidateRestoreCode(new ValidateRestoreCodeModel
        {
            Email = model.Email,
            RestoreCode = model.RestoreCode
        });
        if (validationResult.IsSuccess)
        {
            var identityToChangePassword =
                (await _context.Identities.FirstOrDefaultAsync(identity => identity.Email == model.Email))!;
            var hashingResult = PasswordHasher.HashPassword(model.Password);
            identityToChangePassword.PasswordHash = hashingResult.PasswordHash;
            identityToChangePassword.PasswordSalt = hashingResult.PasswordSalt;
            await _context.SaveChangesAsync();
            return new OperationResult<string>
            {
                IsSuccess = true
            };
        }

        return new OperationResult<string>
        {
            IsSuccess = false,
            Error = validationResult.Error
        };
    }

    public async Task<IdentityDAO> UpdateUserAsync(UpdateIdentityDTO user)
    {
        var dbUser = await _context.Identities.FirstOrDefaultAsync(u => u.Email == user.Email);

        dbUser.Name = user.Name ?? dbUser.Name;
        dbUser.Surname = user.Surname ?? dbUser.Surname;
        dbUser.Age = user.Age ?? dbUser.Age;
        dbUser.City = user.City ?? dbUser.City;
        dbUser.Country = user.Country ?? dbUser.Country;

        await _context.SaveChangesAsync();
        
        return dbUser;
    }

    public async Task<List<IdentityDAO>> GetUsers()
    {
        return await _context.Identities.ToListAsync();
    }

    public async Task<bool> CheckUserExistence(string email)
    {
        if (email is not null)
        {
            var dbUser = await _context.Identities.FirstOrDefaultAsync(u => u.Email == email);

            return dbUser is not null;
        }

        return false;
    }

    private string GenerateRestoreCode()
        => string.Join("", Enumerable.Range(0, 6)
            .Select(_ => RandomGenerationUtils.GenerateRandomInt() % 10));
    
}
