using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Mappers
{
    public static class DashboardMapper
    {
        // Procurement mapping
        public static ProcurementSummaryViewModel ToViewModel(this ProcurementSummary dto)
        {
            return new ProcurementSummaryViewModel
            {
                ProcNum = dto.ProcNum,
                JobTypeName = dto.JobTypeName,
                StatusName = dto.StatusName,
                CreatedBy = dto.CreatedBy,
                CreatedDate = dto.CreatedDate,
                TotalAmount = dto.TotalAmount,
            };
        }

        public static List<ProcurementSummaryViewModel> ToViewModelList(
            this List<ProcurementSummary> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Approval mapping
        public static ApprovalSummaryViewModel ToViewModel(this ApprovalSummary dto)
        {
            return new ApprovalSummaryViewModel
            {
                ProcNum = dto.ProcNum,
                DocumentName = dto.DocumentName,
                ApprovalRole = dto.ApprovalRole,
                CreatedDate = dto.CreatedDate,
            };
        }

        public static List<ApprovalSummaryViewModel> ToViewModelList(
            this List<ApprovalSummary> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // JobType mapping
        public static JobTypeCountViewModel ToViewModel(this JobTypeCount dto)
        {
            return new JobTypeCountViewModel
            {
                JobTypeName = dto.JobTypeName,
                Count = dto.Count,
                TotalValue = dto.TotalValue,
            };
        }

        public static List<JobTypeCountViewModel> ToViewModelList(this List<JobTypeCount> dtos)
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Vendor mapping
        public static VendorPerformanceViewModel ToViewModel(this VendorPerformance dto)
        {
            return new VendorPerformanceViewModel
            {
                VendorCode = dto.VendorCode,
                VendorName = dto.VendorName,
                OfferCount = dto.OfferCount,
                SelectedCount = dto.SelectedCount,
            };
        }

        public static List<VendorPerformanceViewModel> ToViewModelList(
            this List<VendorPerformance> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Purchase Requisition mapping
        public static PurchaseRequisitionSummaryViewModel ToViewModel(
            this PurchaseRequisitionSummary dto
        )
        {
            return new PurchaseRequisitionSummaryViewModel
            {
                PrId = dto.PrId,
                PrNumber = dto.PrNumber,
                RequestDate = dto.RequestDate,
                Description = dto.Description,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                ProcurementCount = dto.ProcurementCount,
            };
        }

        public static List<PurchaseRequisitionSummaryViewModel> ToViewModelList(
            this List<PurchaseRequisitionSummary> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Monthly Trend mapping
        public static MonthlyTrendViewModel ToViewModel(this MonthlyTrend dto)
        {
            return new MonthlyTrendViewModel
            {
                Year = dto.Year,
                Month = dto.Month,
                Count = dto.Count,
                TotalValue = dto.TotalValue,
            };
        }

        public static List<MonthlyTrendViewModel> ToViewModelList(this List<MonthlyTrend> dtos)
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Status Count mapping
        public static StatusCountViewModel ToViewModel(this StatusCount dto)
        {
            return new StatusCountViewModel { StatusName = dto.StatusName, Count = dto.Count };
        }

        public static List<StatusCountViewModel> ToViewModelList(this List<StatusCount> dtos)
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // User Activity mapping
        public static UserActivityViewModel ToViewModel(this RecentLoginSummary dto)
        {
            return new UserActivityViewModel
            {
                UserId = dto.UserId,
                FullName = dto.FullName,
                UserName = dto.UserName,
                JobTitle = dto.JobTitle,
                LastLoginAt = dto.LastLoginAt,
                IsOnline = dto.IsOnline,
            };
        }

        public static List<UserActivityViewModel> ToViewModelList(
            this List<RecentLoginSummary> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }

        // Recent Activity mapping
        public static RecentActivityViewModel ToViewModel(this RecentActivityDto dto)
        {
            return new RecentActivityViewModel
            {
                Time = dto.Time,
                User = dto.User!,
                Action = dto.Action,
                Description = dto.Description,
            };
        }

        public static List<RecentActivityViewModel> ToViewModelList(
            this IReadOnlyList<RecentActivityDto> dtos
        )
        {
            return dtos.Select(dto => dto.ToViewModel()).ToList();
        }
    }
}
