using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
  public interface ITenderService {
    Task<IEnumerable<Tender>> GetAllTenderAsync();
    Task<Tender?> GetTenderByIdAsync(string id);
    Task AddTenderAsync(Tender tender);
    Task EditTenderAsync(Tender tender, string id);
    Task DeleteTenderAsync(Tender tender);
  }
}
