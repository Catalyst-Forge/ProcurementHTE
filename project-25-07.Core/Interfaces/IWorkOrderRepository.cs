using project_25_07.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project_25_07.Core.Interfaces {
  public interface IWorkOrderRepository {
    Task<IEnumerable<WorkOrder>> GetAllAsync();
    Task<WorkOrder?> GetByIdAsync(string id);
    Task StoreWorkOrderAsync(WorkOrder wo);
    Task UpdateWorkOrderAsync(WorkOrder wo);
    Task DropWorkOrderAsync(WorkOrder wo);
    Task<List<WoTypes>> GetWoTypesAsync();
    Task<List<Status>> GetStatusesAsync();
    Task<List<Tender>> GetTendersAsync();
  }
}
