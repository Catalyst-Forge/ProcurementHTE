using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
  public interface ITenderRepository {
    Task<IEnumerable<Tender>> GetAllAsync();
    Task<Tender?> GetByIdAsync(string id);
    Task CreateTenderAsync(Tender tender);
    Task UpdateTenderAsync(Tender tender);
    Task DropTenderAsync(Tender tender);
  }
}
