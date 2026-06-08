namespace ProcurementHTE.Core.Utils
{
    using System;
    using System.Globalization;

    public static partial class IndonesianTerbilangHelper
    {
        public static string TerbilangInteger(long value)
        {
            if (value == 0)
                return "nol";

            if (value < 0)
                return "minus " + TerbilangInteger(Math.Abs(value));

            string[] angkaDasar =
            {
                "", // dummy index 0 (tidak dipakai)
                "Satu", // 1
                "Dua", // 2
                "Tiga", // 3
                "Empat", // 4
                "Lima", // 5
                "Enam", // 6
                "Tujuh", // 7
                "Delapan", // 8
                "Sembilan", // 9
                "Sepuluh", // 10
                "Sebelas", // 11
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
