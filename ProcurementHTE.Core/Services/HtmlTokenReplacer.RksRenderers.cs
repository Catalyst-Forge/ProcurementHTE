using System.Text;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateRKSJangkaWaktuList(Procurement proc)
        {
            var sb = new StringBuilder();
            var jumlahHari = (int)(proc.EndDate.Date - proc.StartDate.Date).TotalDays;
            var terbilangHari = jumlahHari.ToTerbilang();
            var start = proc.StartDate.ToString("d MMMM yyyy", Id);
            var end = proc.EndDate.ToString("d MMMM yyyy", Id);

            if (proc.JobType!.TypeName == "Moving")
            {
                AppendWorkDurationRows(sb, jumlahHari, terbilangHari, start, end);
                sb.AppendLine(
                    "<li>Permohonan perpanjangan Jangka Waktu Pelaksanaan Pekerjaan dan Jangka Waktu Kontrak harus diajukan tertulis oleh salah satu <strong>PIHAK</strong> kepada <strong>PIHAK</strong> lainnya yang dilengkapi dengan justifikasi dan data pendukungnya yang selanjutnya akan dituangkan ke dalam Addendum <strong>KONTRAK</strong> dan disetujui oleh <strong>PARA PIHAK</strong>.</li>"
                );
            }

            if (proc.JobType.TypeName == "Angkutan")
            {
                AppendWorkDurationRows(sb, jumlahHari, terbilangHari, start, end);
                sb.AppendLine(
                    "<li>Permohonan perpanjangan Jangka Waktu Pelaksanaan Pekerjaan dan Jangka Waktu Kontrak harus diajukan tertulis oleh salah satu PIHAK kepada </strong>PIHAK</strong> lainnya yang dilengkapi dengan justifikasi dan data pendukungnya yang selanjutnya akan dituangkan ke dalam Addendum <strong>KONTRAK</strong> dan disetujui oleh <strong>PARA PIHAK</strong></li>"
                );
            }

            if (proc.JobType.TypeName == "StandBy")
            {
                sb.AppendLine(
                    $"<li>Masa sewa adalah selama {jumlahHari} ({terbilangHari}) Hari Kalender, terhitung sejak tanggal {start} sampai dengan tanggal {end}.</li>"
                );
                sb.AppendLine(
                    "<li>Apabila dianggap perlu, <strong>PERUSAHAAN</strong> berhak memperpanjang Masa Sewa menurut Kontrak Kerja untuk jangka waktu tertentu terhitung dari tanggal berakhirnya Masa Sewa.</li>"
                );
                sb.AppendLine(
                    "<li>Permohonan perpanjangan Masa Sewa harus diajukan tertulis oleh salah satu <strong>PIHAK</strong> kepada <strong>PIHAK</strong> lainnya yang dilengkapi dengan justifikasi dan data pendukungnya yang selanjutnya akan dituangkan ke dalam Addendum <strong>KONTRAK</strong> dan disetujui oleh <strong>PERUSAHAAN</strong> dan <strong>MITRA KERJA</strong>.</li>"
                );
            }

            return sb.ToString();
        }

        private static string GenerateRKSSyaratList(Procurement proc)
        {
            if (proc.JobType!.TypeName != "Moving")
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("<li>");
            sb.AppendLine("  Kelengkapan Alat Berat");
            sb.AppendLine(
                "  <p class='mb-0'>MITRA KERJA harus menyediakan alat berat yang terdiri sebagai berikut:</p>"
            );
            sb.AppendLine("  <ol class='sub-list'>");
            sb.AppendLine("    <li>Operator dan Helper wajib memiliki CSMS</li>");
            sb.AppendLine(
                "    <li>Peralatan penunjang termasuk di dalamnya tetapi tidak terbatas pada rantai-rantai pengikat/<em>chain binder</em></li>"
            );
            sb.AppendLine("  </ol>");
            sb.AppendLine("</li>");
            return sb.ToString();
        }

        private static void AppendWorkDurationRows(
            StringBuilder sb,
            int jumlahHari,
            string terbilangHari,
            string start,
            string end
        )
        {
            sb.AppendLine(
                $"<li>Jangka Waktu Pelaksanaan Pekerjaan adalah selama {jumlahHari} ({terbilangHari}) Hari Kalender, mulai tanggal {start} sampai dengan tanggal {end}, terhitung sejak Surat Perintah Melaksanakan Pekerjaan (SPMP) sampai dengan diterbitkannya Berita Acara Penyelesaian Pekerjaan dan/atau Berita Acara Serah Terima Pekerjaan dan telah ditandatangani oleh <strong>PERUSAHAAN</strong> dan <strong>MITRA KERJA</strong>.</li>"
            );
            sb.AppendLine(
                "<li>Apabila dianggap perlu, <strong>PERUSAHAAN</strong> berhak memperpanjang Jangka Waktu Pelaksanaan Pekerjaan menurut Kontrak untuk jangka waktu tertentu terhitung dari tanggal berakhirnya Jangka Waktu Pelaksanaan Pekerjaan.</li>"
            );
        }
    }
}
