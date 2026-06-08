using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class UserActivityViewModel
        {
            public string UserId { get; set; } = string.Empty;
    
            [DisplayName("Full Name")]
            public string FullName { get; set; } = string.Empty;
    
            [DisplayName("Username")]
            public string UserName { get; set; } = string.Empty;
    
            [DisplayName("Job Title")]
            public string? JobTitle { get; set; }
    
            [DisplayName("Last Login")]
            public DateTime LastLoginAt { get; set; }
    
            [DisplayName("Online Status")]
            public bool IsOnline { get; set; }
    
            [DisplayName("Status")]
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
}

