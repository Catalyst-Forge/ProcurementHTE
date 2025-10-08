using project_25_07.Core.Models;

namespace project_25_07.Core.Interfaces {
  public interface ITenderService {
    Task<IEnumerable<Tender>> GetAllTenderAsync();
    Task<Tender?> GetTenderByIdAsync(string id);
    Task AddTenderAsync(Tender tender);
    Task EditTenderAsync(Tender tender, string id);
    Task DeleteTenderAsync(Tender tender);
  }
}
