using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
    public partial class _1360alpha : Migration
========
    public partial class Alpha1 : Migration
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true, computedColumnSql: "CONCAT([FirstName], ' ', [LastName])"),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeId);
                });

            migrationBuilder.CreateTable(
                name: "JobTypes",
                columns: table => new
                {
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypes", x => x.JobTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ProfitLossSelectedVendors",
                columns: table => new
                {
                    ProfitLossSelectedVendorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLossSelectedVendors", x => x.ProfitLossSelectedVendorId);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    TenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenderName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Information = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.TenderId);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VendorCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NPWP = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.VendorId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTypeDocuments",
                columns: table => new
                {
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcurementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WoNumLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateLetter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    From = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkOrderLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WBS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateRequired = table.Column<DateTime>(type: "datetime2", nullable: true),
                    XS1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileWorkOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requester = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Approved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WoTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.WorkOrderId);
                    table.UniqueConstraint("AK_WorkOrders_WoNum", x => x.WoNum);
                    table.ForeignKey(
                        name: "FK_WorkOrders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId");
                    table.ForeignKey(
                        name: "FK_WorkOrders_WoTypes_WoTypeId",
                        column: x => x.WoTypeId,
                        principalTable: "WoTypes",
                        principalColumn: "WoTypeId");
                });

            migrationBuilder.CreateTable(
                name: "WoTypesDocuments",
                columns: table => new
                {
                    WoTypeDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
========
                    JobTypeDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsUploadRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypeDocuments", x => x.JobTypeDocumentId);
                    table.ForeignKey(
                        name: "FK_JobTypeDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTypeDocuments_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Procurements",
                columns: table => new
                {
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SelectedVendorFinalOffer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SelectedVendorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
========
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcNum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SpkNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    JobTypeOther = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectRegion = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    AccrualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RealizationAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PotentialAccrualDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpmpNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MemoNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SelectedVendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VendorSphNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RaNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProjectCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LtcName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PicOpsUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnalystHteSignerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssistantManagerSignerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ManagerSignerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procurements", x => x.ProcurementId);
                    table.ForeignKey(
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                        name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                        column: x => x.SelectedVendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitLosses_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoDetails",
                columns: table => new
                {
                    WoDetailId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDetails", x => x.WoDetailId);
========
                        name: "FK_Procurements_AspNetUsers_AnalystHteSignerUserId",
                        column: x => x.AnalystHteSignerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procurements_AspNetUsers_AssistantManagerSignerUserId",
                        column: x => x.AssistantManagerSignerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procurements_AspNetUsers_ManagerSignerUserId",
                        column: x => x.ManagerSignerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procurements_AspNetUsers_PicOpsUserId",
                        column: x => x.PicOpsUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                    table.ForeignKey(
                        name: "FK_Procurements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procurements_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                        name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoOffers",
                columns: table => new
                {
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemPenawaran = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoOffers", x => x.WoOfferId);
                    table.ForeignKey(
                        name: "FK_WoOffers_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
========
                        name: "FK_Procurements_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                });

            migrationBuilder.CreateTable(
                name: "DocumentApprovals",
                columns: table => new
                {
                    DocumentApprovalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTypeDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovals", x => x.DocumentApprovalId);
                    table.ForeignKey(
                        name: "FK_DocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentApprovals_JobTypeDocuments_JobTypeDocumentId",
                        column: x => x.JobTypeDocumentId,
                        principalTable: "JobTypeDocuments",
                        principalColumn: "JobTypeDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcDetails",
                columns: table => new
                {
                    ProcDetailId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DetailKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDetails", x => x.ProcDetailId);
                    table.ForeignKey(
                        name: "FK_ProcDetails_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcDetails_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcDocuments",
                columns: table => new
                {
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    QrText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    QrObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDocuments", x => x.ProcDocumentId);
                    table.ForeignKey(
                        name: "FK_ProcDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocuments_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcOffers",
                columns: table => new
                {
                    ProcOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemPenawaran = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcOffers", x => x.ProcOfferId);
                    table.ForeignKey(
                        name: "FK_ProcOffers_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitLosses",
                columns: table => new
                {
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SelectedVendorFinalOffer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SelectedVendorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLosses", x => x.ProfitLossId);
                    table.ForeignKey(
                        name: "FK_ProfitLosses_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                        column: x => x.SelectedVendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcDocumentApprovals",
                columns: table => new
                {
                    ProcDocumentApprovalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDocumentApprovals", x => x.ProcDocumentApprovalId);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_ProcDocuments_ProcDocumentId",
                        column: x => x.ProcDocumentId,
                        principalTable: "ProcDocuments",
                        principalColumn: "ProcDocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorOffers",
                columns: table => new
                {
                    VendorOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NoLetter = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorOffers", x => x.VendorOfferId);
                    table.ForeignKey(
                        name: "FK_VendorOffers_ProcOffers_ProcOfferId",
                        column: x => x.ProcOfferId,
                        principalTable: "ProcOffers",
                        principalColumn: "ProcOfferId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorOffers_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorOffers_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfitLossItems",
                columns: table => new
                {
                    ProfitLossItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TarifAwal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TarifAdd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KmPer25 = table.Column<int>(type: "int", nullable: false),
                    OperatorCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcOfferId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLossItems", x => x.ProfitLossItemId);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_ProcOffers_ProcOfferId",
                        column: x => x.ProcOfferId,
                        principalTable: "ProcOffers",
                        principalColumn: "ProcOfferId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                        column: x => x.ProfitLossId,
                        principalTable: "ProfitLosses",
                        principalColumn: "ProfitLossId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfitLossItems",
                columns: table => new
                {
                    ProfitLossItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TarifAwal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TarifAdd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KmPer25 = table.Column<int>(type: "decimal(18,2)", nullable: false),
                    OperatorCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLossItems", x => x.ProfitLossItemId);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                        column: x => x.ProfitLossId,
                        principalTable: "ProfitLosses",
                        principalColumn: "ProfitLossId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_WoOffers_WoOfferId",
                        column: x => x.WoOfferId,
                        principalTable: "WoOffers",
                        principalColumn: "WoOfferId");
                });

            migrationBuilder.CreateTable(
                name: "VendorOffers",
                columns: table => new
                {
                    VendorOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NoLetter = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorOffers", x => x.VendorOfferId);
                    table.ForeignKey(
                        name: "FK_VendorOffers_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorOffers_WoOffers_WoOfferId",
                        column: x => x.WoOfferId,
                        principalTable: "WoOffers",
                        principalColumn: "WoOfferId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorOffers_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovals_JobTypeDocumentId",
                table: "DocumentApprovals",
                column: "JobTypeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovals_RoleId",
                table: "DocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypeDocuments_DocumentTypeId",
                table: "JobTypeDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypeDocuments_JobTypeId",
                table: "JobTypeDocuments",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDetails_ProcurementId",
                table: "ProcDetails",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDetails_VendorId",
                table: "ProcDetails",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ApproverId",
                table: "ProcDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcDocumentId",
                table: "ProcDocumentApprovals",
                column: "ProcDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcurementId",
                table: "ProcDocumentApprovals",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_RoleId",
                table: "ProcDocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocuments_DocumentTypeId",
                table: "ProcDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocuments_ProcurementId",
                table: "ProcDocuments",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcOffers_ProcurementId",
                table: "ProcOffers",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "AK_Procurements_ProcNum",
                table: "Procurements",
                column: "ProcNum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AnalystHteSignerUserId",
                table: "Procurements",
                column: "AnalystHteSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AssistantManagerSignerUserId",
                table: "Procurements",
                column: "AssistantManagerSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_JobTypeId",
                table: "Procurements",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ManagerSignerUserId",
                table: "Procurements",
                column: "ManagerSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PicOpsUserId",
                table: "Procurements",
                column: "PicOpsUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_StatusId",
                table: "Procurements",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_UserId_CreatedAt",
                table: "Procurements",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLosses_ProcurementId",
                table: "ProfitLosses",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLosses_SelectedVendorId",
                table: "ProfitLosses",
                column: "SelectedVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_ProcOfferId",
                table: "ProfitLossItems",
                column: "ProcOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_ProfitLossId",
                table: "ProfitLossItems",
                column: "ProfitLossId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_ProfitLossId",
                table: "ProfitLossItems",
                column: "ProfitLossId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_WoOfferId",
                table: "ProfitLossItems",
                column: "WoOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_ProcOfferId",
                table: "VendorOffers",
                column: "ProcOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_ProcurementId",
                table: "VendorOffers",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_VendorId",
                table: "VendorOffers",
                column: "VendorId");

            migrationBuilder.CreateIndex(
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                name: "IX_VendorOffers_WoOfferId",
                table: "VendorOffers",
                column: "WoOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_WorkOrderId",
                table: "VendorOffers",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
========
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                name: "IX_Vendors_VendorCode",
                table: "Vendors",
                column: "VendorCode",
                unique: true);
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs

            migrationBuilder.CreateIndex(
                name: "IX_WoDetails_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocApprovals_Role_Status",
                table: "WoDocumentApprovals",
                columns: new[] { "RoleId", "Status" })
                .Annotation("SqlServer:Include", new[] { "WoDocumentId", "WorkOrderId", "Level", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_ApproverId",
                table: "WoDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WorkOrderId",
                table: "WoDocumentApprovals",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "UX_WoDocApprovals_Doc_Level_Seq",
                table: "WoDocumentApprovals",
                columns: new[] { "WoDocumentId", "Level", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_DocumentTypeId",
                table: "WoDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_QrText",
                table: "WoDocuments",
                column: "QrText");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId_CreatedAt",
                table: "WoDocuments",
                columns: new[] { "WorkOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId_DocumentTypeId_Status",
                table: "WoDocuments",
                columns: new[] { "WorkOrderId", "DocumentTypeId", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoOffers_WorkOrderId",
                table: "WoOffers",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_StatusId",
                table: "WorkOrders",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_UserId_CreatedAt_Covering",
                table: "WorkOrders",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "WoNum", "Description", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WoTypeId",
                table: "WorkOrders",
                column: "WoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_DocumentTypeId",
                table: "WoTypesDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_WoTypeId",
                table: "WoTypesDocuments",
                column: "WoTypeId");
========
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DocumentApprovals");

            migrationBuilder.DropTable(
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
========
                name: "ProcDetails");

            migrationBuilder.DropTable(
                name: "ProcDocumentApprovals");

            migrationBuilder.DropTable(
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs
                name: "ProfitLossItems");

            migrationBuilder.DropTable(
                name: "ProfitLossSelectedVendors");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Tenders");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "VendorOffers");

            migrationBuilder.DropTable(
                name: "JobTypeDocuments");

            migrationBuilder.DropTable(
                name: "ProcDocuments");

            migrationBuilder.DropTable(
<<<<<<<< HEAD:ProcurementHTE.Infrastructure/Migrations/20251112221509_1.36.0-alpha.cs
                name: "WoTypesDocuments");

            migrationBuilder.DropTable(
                name: "ProfitLosses");

            migrationBuilder.DropTable(
                name: "WoOffers");
========
                name: "ProfitLosses");
>>>>>>>> development:ProcurementHTE.Infrastructure/Migrations/20251114003552_Alpha1.cs

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ProcOffers");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Procurements");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "JobTypes");

            migrationBuilder.DropTable(
                name: "Statuses");
        }
    }
}
