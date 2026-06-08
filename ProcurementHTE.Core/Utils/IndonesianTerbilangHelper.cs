namespace ProcurementHTE.Core.Utils
{
    using System;
    using System.Globalization;

    public static partial class IndonesianTerbilangHelper
    {
        public static string ToTerbilangRupiah(
            this decimal amount,
            bool includeCurrencyWord = true,
            bool includeSen = false
        )
        {
            return ToTerbilang(amount, includeCurrencyWord, includeSen);
        }

        public static string ToTerbilangRupiah(this int amount, bool includeCurrencyWord = true)
        {
            return ToTerbilang(amount, includeCurrencyWord, includeSen: false);
        }

        public static string ToTerbilangRupiah(this long amount, bool includeCurrencyWord = true)
        {
            return ToTerbilang(amount, includeCurrencyWord, includeSen: false);
        }

        public static string ToTerbilang(this int value)
        {
            return TerbilangInteger(value);
        }

        public static string ToTerbilang(this long value)
        {
            return TerbilangInteger(value);
        }

        public static string ToTerbilangHari(
            this DateTime startDate,
            DateTime endDate,
            bool includeUnitWord = true
        )
        {
            var start = startDate.Date;
            var end = endDate.Date;

            if (end < start)
                throw new ArgumentException(
                    "End date tidak boleh sebelum start date.",
                    nameof(endDate)
                );

            var totalDays = (int)(end - start).TotalDays;
            var terbilang = TerbilangInteger(totalDays);

            return includeUnitWord ? $"{terbilang} hari" : terbilang;
        }

        private static string ToTerbilang(decimal amount, bool includeCurrencyWord, bool includeSen)
        {
            // Guard sederhana biar gak overflow saat cast ke long
            if (amount > long.MaxValue || amount < long.MinValue)
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Nilai terlalu besar untuk dikonversi ke terbilang."
                );

            var isNegative = amount < 0;
            amount = Math.Abs(amount);

            // Pisahkan bagian bulat dan pecahan
            var integerPart = (long)Math.Truncate(amount);
            var fractional = amount - integerPart;

            // Sen = 2 digit di belakang koma
            var sen = 0;
            if (includeSen)
            {
                sen = (int)Math.Round(fractional * 100m, 0, MidpointRounding.AwayFromZero);

                // Kalau hasil pembulatan = 100 sen, naikkan ke rupiah
                if (sen == 100)
                {
                    integerPart += 1;
                    sen = 0;
                }
            }

            // Terbilang untuk bagian bulat
            var result = TerbilangInteger(integerPart);

            if (includeCurrencyWord)
            {
                result += " rupiah";
            }

            // Tambah sen kalau diminta dan ada nilainya
            if (includeSen && sen > 0)
            {
                result += " " + TerbilangInteger(sen) + " sen";
            }

            if (isNegative)
            {
                result = "minus " + result;
            }

            return result.Trim();
        }
    }
}
