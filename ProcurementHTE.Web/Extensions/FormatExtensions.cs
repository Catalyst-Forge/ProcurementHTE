using System.Globalization;

namespace ProcurementHTE.Web.Extensions
{
    public static class FormatExtensions
    {
        private static readonly CultureInfo IdCulture = new CultureInfo("id-ID");

        // Extension untuk decimal
        public static string ToIDR(this decimal value)
        {
            return $"Rp {value.ToString("N0", IdCulture)}";
        }

        // Extension untuk decimal nullable
        public static string ToIDR(this decimal? value)
        {
            return value.HasValue ? $"Rp {value.Value.ToString("N0", IdCulture)}" : "Rp 0";
        }

        // Extension untuk int
        public static string ToIDR(this int value)
        {
            return $"Rp {value.ToString("N0", IdCulture)}";
        }

        // Extension untuk int nullable
        public static string ToIDR(this int? value)
        {
            return value.HasValue ? $"Rp {value.Value.ToString("N0", IdCulture)}" : "Rp 0";
        }

        // Extension untuk double
        public static string ToIDR(this double value)
        {
            return $"Rp {value.ToString("N0", IdCulture)}";
        }

        // Extension untuk double nullable
        public static string ToIDR(this double? value)
        {
            return value.HasValue ? $"Rp {value.Value.ToString("N0", IdCulture)}" : "Rp 0";
        }

        // Extension untuk DateTime
        public static string ToTanggalIndonesia(this DateTime date, string format = "dd MMMM yyyy")
        {
            return date.ToString(format, IdCulture);
        }

        // Extension untuk DateTime nullable
        public static string ToTanggalIndonesia(this DateTime? date, string format = "dd MMMM yyyy")
        {
            return date.HasValue ? date.Value.ToString(format, IdCulture) : "-";
        }

        public static string ToTanggalLengkap(this DateTime date)
        {
            return date.ToString("dddd, dd MMMM yyyy", IdCulture);
        }

        public static string ToTanggalLengkap(this DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dddd, dd MMMM yyyy", IdCulture) : "-";
        }

        public static string ToTanggalWaktu(this DateTime date)
        {
            return date.ToString("dd MMMM yyyy, HH:mm", IdCulture) + " WIB";
        }

        public static string ToTanggalWaktu(this DateTime? date)
        {
            return date.HasValue
                ? date.Value.ToString("dd MMMM yyyy, HH:mm", IdCulture) + " WIB"
                : "-";
        }

        public static string ToTanggalSlash(this DateTime date)
        {
            return date.ToString("dd/MM/yyyy", IdCulture);
        }

        public static string ToTanggalSlash(this DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy", IdCulture) : "-";
        }

        // Extension untuk Persen - decimal
        public static string ToPersen(this decimal value, int decimals = 2)
        {
            return value.ToString($"N{decimals}", IdCulture) + "%";
        }

        // Extension untuk Persen - decimal nullable
        public static string ToPersen(this decimal? value, int decimals = 2)
        {
            return value.HasValue ? value.Value.ToString($"N{decimals}", IdCulture) + "%" : "0%";
        }

        // Extension untuk Persen - double
        public static string ToPersen(this double value, int decimals = 2)
        {
            return value.ToString($"N{decimals}", IdCulture) + "%";
        }

        // Extension untuk Persen - double nullable
        public static string ToPersen(this double? value, int decimals = 2)
        {
            return value.HasValue ? value.Value.ToString($"N{decimals}", IdCulture) + "%" : "0%";
        }

        // Extension untuk Persen - int (biasanya sudah dalam bentuk persen)
        public static string ToPersen(this int value)
        {
            return value.ToString("N0", IdCulture) + "%";
        }

        // Extension untuk Persen - int nullable
        public static string ToPersen(this int? value)
        {
            return value.HasValue ? value.Value.ToString("N0", IdCulture) + "%" : "0%";
        }

        // Extension untuk mengubah decimal ke persen (jika nilai dalam bentuk 0.15 = 15%)
        public static string ToPersenFromDecimal(this decimal value, int decimals = 2)
        {
            return (value * 100).ToString($"N{decimals}", IdCulture) + "%";
        }

        // Extension untuk mengubah decimal nullable ke persen
        public static string ToPersenFromDecimal(this decimal? value, int decimals = 2)
        {
            return value.HasValue
                ? (value.Value * 100).ToString($"N{decimals}", IdCulture) + "%"
                : "0%";
        }

        // Extension untuk mengubah double ke persen (jika nilai dalam bentuk 0.15 = 15%)
        public static string ToPersenFromDecimal(this double value, int decimals = 2)
        {
            return (value * 100).ToString($"N{decimals}", IdCulture) + "%";
        }

        // Extension untuk mengubah double nullable ke persen
        public static string ToPersenFromDecimal(this double? value, int decimals = 2)
        {
            return value.HasValue
                ? (value.Value * 100).ToString($"N{decimals}", IdCulture) + "%"
                : "0%";
        }
    }
}
