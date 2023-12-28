using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;

namespace ChummyFoodBack.Feature.IdentityManagement;

public interface IIdentityService
{
    public Task RegisterUser(IdentityModel identityModel, string role);

    public Task<OperationValuedResult<IdentityGeneratedCreadentials, string>> RegisterUserWithoutPassword(string email);
    public bool CheckIdentitiesEqual(IdentityModel requestModel, IdentityDAO persistedIdentity);

    public Task<OperationResult<string>> ValidateRestoreCode(ValidateRestoreCodeModel model);
    public Task<OperationResult<StartPasswordRestoreErrorModel>> StartPasswordRestoreForEmail(TryPasswordCheckModel passwordCheckModel);

    public Task<OperationResult<string>> RestorePassword(ChangePasswordModel model);

    public Task<IdentityDAO> UpdateUserAsync(UpdateIdentityDTO user);

    public Task<List<IdentityDAO>> GetUsers();

    public Task<bool> CheckUserExistence(string email);
}
