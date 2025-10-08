using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
  public class TenderService : ITenderService {
    private readonly ITenderRepository _tenderRepository;

    public TenderService(ITenderRepository tenderRepository) {
      _tenderRepository = tenderRepository;
    }

    public async Task<IEnumerable<Tender>> GetAllTenderAsync() {
      return await _tenderRepository.GetAllAsync();
    }

    public async Task<Tender?> GetTenderByIdAsync(string id) {
      return await _tenderRepository.GetByIdAsync(id);
    }

    public async Task AddTenderAsync(Tender tender) {
      if (string.IsNullOrEmpty(tender.TenderName)) {
          throw new ArgumentException("Tender name cannot be empty");
      }

      await _tenderRepository.CreateTenderAsync(tender);
    }

    public async Task EditTenderAsync(Tender tender, string id) {
      if (tender == null) {
        throw new ArgumentNullException(nameof(tender));
      }

      var existingTender = await _tenderRepository.GetByIdAsync(id);
      if (existingTender == null) {
        throw new KeyNotFoundException($"Tender with ID {id} not found");
      }

      existingTender.TenderName = tender.TenderName;
      existingTender.Price = tender.Price;
      existingTender.Information = tender.Information;

      await _tenderRepository.UpdateTenderAsync(existingTender);
    }

    public async Task DeleteTenderAsync(Tender tender) {
      if (tender == null) {
        throw new ArgumentException(nameof(tender));
      }

      await _tenderRepository.DropTenderAsync(tender);
    }
  }
}
