namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProcurementSummary
    {
        public string ProcNum { get; set; } = string.Empty;
        public string JobTypeName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ApprovalSummary
    {
        public string ProcNum { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string ApprovalRole { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int DaysWaiting => (DateTime.Now - CreatedDate).Days;
    }

    public class JobTypeCount
    {
        public string JobTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class VendorPerformance
    {
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public int OfferCount { get; set; }
        public int SelectedCount { get; set; }
        public decimal WinRate =>
            OfferCount > 0 ? Math.Round((decimal)SelectedCount / OfferCount * 100, 2) : 0;
    }

    public class PurchaseRequisitionSummary
    {
        public string PrId { get; set; } = string.Empty;
        public string PrNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ProcurementCount { get; set; }
    }

    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public string MonthYear => $"{GetMonthName(Month)} {Year}";

        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMM");
        }
    }

    public class StatusCount
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentLoginSummary
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsOnline { get; set; }
        public string StatusTime => GetStatusTime();

        private string GetStatusTime()
        {
            var timeSpan = DateTime.Now - LastLoginAt;
            if (IsOnline)
            {
                if (timeSpan.TotalMinutes < 1)
                    return "Online - Baru saja";
                if (timeSpan.TotalMinutes < 60)
                    return $"Online - {(int)timeSpan.TotalMinutes} menit yang lalu";
                return $"Online - {(int)timeSpan.TotalHours} jam yang lalu";
            }
            else
            {
                if (timeSpan.TotalMinutes < 60)
                    return $"Offline - {(int)timeSpan.TotalMinutes} menit yang lalu";
                if (timeSpan.TotalHours < 24)
                    return $"Offline - {(int)timeSpan.TotalHours} jam yang lalu";
                if (timeSpan.TotalDays < 7)
                    return $"Offline - {(int)timeSpan.TotalDays} hari yang lalu";
                return $"Offline - {LastLoginAt:dd MMM yyyy}";
            }
        }
    }

    public class OnlineUserSummary
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string TimeSinceLogin => GetTimeSinceLogin();

        private string GetTimeSinceLogin()
        {
            var timeSpan = DateTime.Now - LastLoginAt;
            if (timeSpan.TotalMinutes < 1)
                return "Baru saja";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} menit yang lalu";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} jam yang lalu";
            return $"{(int)timeSpan.TotalDays} hari yang lalu";
        }
    }
}
