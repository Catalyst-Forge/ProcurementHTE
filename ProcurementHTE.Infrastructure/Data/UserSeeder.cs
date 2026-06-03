using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager)
        {
            var users = new (
                string Username,
                string Email,
                string FirstName,
                string LastName,
                string Password,
                string Role
            )[]
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
                    "Assistant Manager HTE"
                ),
                (
                    "vicepresident",
                    "vp@example.com",
                    "Vice",
                    "President",
                    "VicePresident123!",
                    "Vice President"
                ),
                (
                    "opdir",
                    "opdir@example.com",
                    "Operation",
                    "Director",
                    "OpDir123!",
                    "Operation Director"
                ),
                (
                    "presdir",
                    "presdir@example.com",
                    "President",
                    "Director",
                    "PresDir123!",
                    "President Director"
                ),
                ("board", "board@example.com", "Dewan", "Direksi", "Board123!", "Dewan Direksi"),
                (
                    "komisaris",
                    "komisaris@example.com",
                    "Commisioner",
                    "",
                    "Komisaris123!",
                    "Dewan Komisaris"
                ),
                ("hse", "hse@example.com", "HSE", "", "Hse1234!", "HSE"),
                (
                    "scm",
                    "scm@example.com",
                    "Supply Chain",
                    "Management",
                    "Scm1234!",
                    "Supply Chain Management"
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
                    "Assistant Manager HTE"
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
                // Direksi dengan nama asli
                (
                    "faried",
                    "faried.iskandar@pertamina-pdc.com",
                    "Faried",
                    "Iskandar Dozyn",
                    "Faried123!",
                    "President Director"
                ),
                (
                    "apriandy",
                    "apriandy.zainuddin@pertamina-pdc.com",
                    "Apriandy",
                    "Zainuddin",
                    "Apriandy123!",
                    "Operation Director"
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

            foreach (var u in users)
            {
                await UserSeed(
                    userManager,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Password,
                    u.Role
                );
            }
        }

        private static async Task UserSeed(
            UserManager<User> userManager,
            string userName,
            string email,
            string firstName,
            string lastName,
            string password,
            string role
        )
        {
            // Check if user already exists by email OR username
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = await userManager.FindByNameAsync(userName);
            }
            
            if (user == null)
            {
                user = new User
                {
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    FirstName = firstName,
                    LastName = lastName,
                    JobTitle = role,
                    IsActive = true,
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                    throw new Exception(
                        $"Gagal membuat user {email}: {string.Join(", ", createResult.Errors.Select(err => err.Description))}"
                    );
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                    throw new Exception(
                        $"Gagal menambahkan role {role} ke user {userName}: {string.Join(", ", addRoleResult.Errors.Select(err => err.Description))}"
                    );
            }
        }
    }
}
