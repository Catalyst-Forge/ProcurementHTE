using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(User user);
    }
}
