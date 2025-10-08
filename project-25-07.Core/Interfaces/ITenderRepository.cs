using project_25_07.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project_25_07.Core.Interfaces {
  public interface ITenderRepository {
    Task<IEnumerable<Tender>> GetAllAsync();
    Task<Tender?> GetByIdAsync(string id);
    Task CreateTenderAsync(Tender tender);
    Task UpdateTenderAsync(Tender tender);
    Task DropTenderAsync(Tender tender);
  }
}
