using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly AppDbContext _context;

        public WorkOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WorkOrder>> GetAllAsync()
        {
            return await _context
                .WorkOrders.Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.Vendor)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WorkOrder?> GetByIdAsync(string id)
        {
            return await _context
                .WorkOrders.Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.Vendor)
                .FirstOrDefaultAsync(t => t.WorkOrderId == id);
        }

        public async Task<List<WoTypes>> GetWoTypesAsync()
        {
            return await _context.WoTypes.OrderBy(wt => wt.TypeName).ToListAsync();
        }

        public async Task<WoTypes?> GetWoTypeByIdAsync(int id)
        {
            return await _context.WoTypes.FirstOrDefaultAsync(x => x.WoTypeId == id);
        }

        public async Task<List<Status>> GetStatusesAsync()
        {
            return await _context.Statuses.OrderBy(s => s.StatusName).ToListAsync();
        }

        public async Task StoreWorkOrderAsync(WorkOrder wo)
        {
            await _context.AddAsync(wo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWorkOrderAsync(WorkOrder wo)
        {
            //_context.Entry(wo).State = EntityState.Modified;
            //await _context.SaveChangesAsync();

            if (wo == null)
            {
                throw new ArgumentNullException(nameof(wo));
            }

            try
            {
                _context.WorkOrders.Update(wo);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _context.WorkOrders.AnyAsync(w =>
                    w.WorkOrderId == wo.WorkOrderId
                );
                if (!exists)
                {
                    throw new KeyNotFoundException(
                        $"Work Order dengan ID {wo.WorkOrderId} tidak ditemukan"
                    );
                }
                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi",
                    ex
                );
            }
        }

        public async Task DropWorkOrderAsync(WorkOrder wo)
        {
            _context.WorkOrders.Remove(wo);
            await _context.SaveChangesAsync();
        }
    }
}
