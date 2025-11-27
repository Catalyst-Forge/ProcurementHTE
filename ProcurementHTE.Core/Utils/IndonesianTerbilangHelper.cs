namespace ProcurementHTE.Core.Utils
{
    using System;
    using System.Globalization;

    public static class IndonesianTerbilangHelper
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

        public static string TerbilangInteger(long value)
        {
            if (value == 0)
                return "nol";

            if (value < 0)
                return "minus " + TerbilangInteger(Math.Abs(value));

            string[] angkaDasar =
            {
                "", // dummy index 0 (tidak dipakai)
                "satu", // 1
                "dua", // 2
                "tiga", // 3
                "empat", // 4
                "lima", // 5
                "enam", // 6
                "tujuh", // 7
                "delapan", // 8
                "sembilan", // 9
                "sepuluh", // 10
                "sebelas", // 11
            };

            string result;

            if (value < 12)
            {
                result = angkaDasar[value];
            }
            else if (value < 20)
            {
                // 12 - 19 -> "x belas"
                result = TerbilangInteger(value - 10) + " belas";
            }
            else if (value < 100)
            {
                // 20 - 99 -> "x puluh y"
                result = TerbilangInteger(value / 10) + " puluh";
                long sisa = value % 10;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 200)
            {
                // 100 - 199 -> "seratus x"
                result = "seratus";
                long sisa = value - 100;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 1000)
            {
                // 200 - 999 -> "x ratus y"
                result = TerbilangInteger(value / 100) + " ratus";
                long sisa = value % 100;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 2000)
            {
                // 1000 - 1999 -> "seribu x"
                result = "seribu";
                long sisa = value - 1000;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 1000000)
            {
                // 2.000 - 999.999 -> "x ribu y"
                result = TerbilangInteger(value / 1000) + " ribu";
                long sisa = value % 1000;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 1000000000)
            {
                // 1.000.000 - 999.999.999 -> "x juta y"
                result = TerbilangInteger(value / 1000000) + " juta";
                long sisa = value % 1000000;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 1000000000000)
            {
                // 1.000.000.000 - 999.999.999.999 -> "x miliar y"
                result = TerbilangInteger(value / 1000000000) + " miliar";
                long sisa = value % 1000000000;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else if (value < 1000000000000000)
            {
                // 1.000.000.000.000 - 999.999.999.999.999 -> "x triliun y"
                result = TerbilangInteger(value / 1000000000000) + " triliun";
                long sisa = value % 1000000000000;

                if (sisa > 0)
                    result += " " + TerbilangInteger(sisa);
            }
            else
            {
                // Di atas ini jarang banget untuk kebutuhan aplikasi umum,
                // fallback ke ToString saja.
                result = value.ToString(CultureInfo.InvariantCulture);
            }

            return result.Trim();
        }
    }
}
