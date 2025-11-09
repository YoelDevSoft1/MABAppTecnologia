using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public interface IUserService
    {
        OperationResult ConfigureAdminUser(string newPassword, string pin);
        OperationResult CreateMABUser();
        OperationResult RemovePasswordRequirement(string username);
    }
}
