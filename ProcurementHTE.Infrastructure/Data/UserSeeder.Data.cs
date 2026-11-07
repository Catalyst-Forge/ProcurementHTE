using ProcurementHTE.Core.Constants;

namespace ProcurementHTE.Infrastructure.Data
{
    public static partial class UserSeeder
    {
        private static (
            string Username,
            string Email,
            string FirstName,
            string LastName,
            string Password,
            string Role
        )[] GetUsers()
        {
            return new[]
            {
                ("admin", "admin@example.com", "Admin", "", "Admin123!", "Admin"),
                ("appo", "appo@example.com", "AP-PO", "", "Appo123!", "AP-PO"),
                (
                    "managerTL",
                    "manager@example.com",
                    "Manager",
                    "Transport & Logistic",
                    "Manager123!",
                    "Manager Transport & Logistic"
                ),
                (
                    "ahte",
                    "AHte@example.com",
                    "Analyst",
                    "HTE & LTS",
                    "AHte123!",
                    "Analyst HTE & LTS"
                ),
                (
                    "operation",
                    "pro.operation@example.com",
                    "Operation",
                    "HTE",
                    "ProOperation123!",
                    "Operation"
                ),
                (
                    "assistantmanagerhte",
                    "assistantmanagerhte@example.com",
                    "Assistant",
                    "Manager",
                    "AssistantManager123!",
                    RoleNames.AsstManager
                ),
                (
                    "vicepresident",
                    "vp@example.com",
                    "Vice",
                    "President",
                    "VicePresident123!",
                    "Vice President"
                ),
                ("naura", "khinsa.naura@pertamina-pdc.com", "Khinsa", "Naura", "Ura12345", "Admin"),
                ("diah", "dyahayusekaragung@gmail.com", "Diah", "Ayu", "DiahAyu123", "Operation"),
                (
                    "heri",
                    "heriwibisono@gmail.con",
                    "Heri",
                    "Wibisono",
                    "Heri1234",
                    "Analyst HTE & LTS"
                ),
                (
                    "yoddy",
                    "yoddi.syafei@pertamina-pdc.com",
                    "Yoddy",
                    "Syafei",
                    "Yoddy123",
                    "Analyst HTE & LTS"
                ),
                (
                    "johanis",
                    "johanis@example.com",
                    "Johanis",
                    "",
                    "Johanis123",
                    "Operation"
                ),
                (
                    "dopiyanto",
                    "dopiyanto@gmail.com",
                    "Dopiyanto",
                    "",
                    "Dopiyanto123",
                    "Analyst HTE & LTS"
                ),
                (
                    "edo",
                    "edopradipta@gmail.com",
                    "Edo",
                    "Pradipta",
                    "EdoPradipta123",
                    RoleNames.AsstManager
                ),
                (
                    "kurniawan",
                    "kurniawan@example.com",
                    "Kurniawan",
                    "",
                    "Kurniawan123",
                    "Manager Transport & Logistic"
                ),
                (
                    "ar",
                    "ar@example.com",
                    "AR",
                    "",
                    "Ar123456!",
                    "AR"
                ),
                (
                    "apinvoice",
                    "apinvoice@example.com",
                    "AP-Invoice",
                    "",
                    "ApInvoice123!",
                    "AP-Invoice"
                ),
                (
                    "agus",
                    "agus.sudjatmoko@pertamina-pdc.com",
                    "Agus",
                    "Sudjatmoko",
                    "Agus1234!",
                    "Vice President"
                ),
            };
        }
    }
}
