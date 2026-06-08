using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class MonthlyTrendViewModel
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
}

