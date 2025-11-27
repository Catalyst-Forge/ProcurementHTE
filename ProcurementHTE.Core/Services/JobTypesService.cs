using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class JobTypesService : IJobTypeService
    {
        private readonly IJobTypeRepository _jobTypeRepository;

        public JobTypesService(IJobTypeRepository jobTypeRepository)
        {
            _jobTypeRepository = jobTypeRepository;
        }

        public Task<PagedResult<JobTypes>> GetAllJobTypessAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            return _jobTypeRepository.GetAllAsync(page, pageSize, search, fields, ct);
        }

        public async Task AddJobTypesAsync(JobTypes jobTypes)
        {
            if (string.IsNullOrEmpty(jobTypes.TypeName))
            {
                throw new ArgumentException("Type Name cannot be empty");
            }

            await _jobTypeRepository.CreateJobTypeAsync(jobTypes);
        }

        public async Task EditJobTypesAsync(JobTypes jobTypes, string jobTypeId)
        {
            if (jobTypes == null)
            {
                throw new ArgumentNullException(nameof(jobTypes));
            }

            var existingJobTypes = await _jobTypeRepository.GetByIdAsync(jobTypeId);
            if (existingJobTypes == null)
            {
                throw new KeyNotFoundException($"Wo Type With ID {jobTypeId}");
            }

            existingJobTypes.TypeName = jobTypes.TypeName;
            existingJobTypes.Description = jobTypes.Description;

            await _jobTypeRepository.UpdateJobTypeAsync(existingJobTypes);
        }

        public async Task DeleteJobTypesAsync(JobTypes jobTypes)
        {
            if (jobTypes == null)
            {
                throw new ArgumentNullException(nameof(jobTypes));
            }
            try
            {
                await _jobTypeRepository.DropJobTypeAsync(jobTypes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[DEBUG] {e}");
            }
        }

        public async Task<JobTypes?> GetJobTypesByIdAsync(string JobTypeId)
        {
            return await _jobTypeRepository.GetByIdAsync(JobTypeId);
        }
    }
}
