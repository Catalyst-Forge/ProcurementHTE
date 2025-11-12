IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [FullName] AS CONCAT([FirstName], ' ', [LastName]),
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [DocumentTypes] (
        [DocumentTypeId] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DocumentTypes] PRIMARY KEY ([DocumentTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [ProfitLossSelectedVendors] (
        [ProfitLossSelectedVendorId] nvarchar(450) NOT NULL,
        [WorkOrderId] nvarchar(max) NOT NULL,
        [VendorId] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProfitLossSelectedVendors] PRIMARY KEY ([ProfitLossSelectedVendorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [Token] nvarchar(512) NOT NULL,
        [DeviceId] nvarchar(128) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [Revoked] bit NOT NULL,
        [RevokedAt] datetime2 NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [Statuses] (
        [StatusId] int NOT NULL IDENTITY,
        [StatusName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Statuses] PRIMARY KEY ([StatusId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [Tenders] (
        [TenderId] nvarchar(450) NOT NULL,
        [TenderName] nvarchar(255) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Information] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Tenders] PRIMARY KEY ([TenderId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [Vendors] (
        [VendorId] nvarchar(450) NOT NULL,
        [VendorCode] nvarchar(450) NOT NULL,
        [VendorName] nvarchar(max) NOT NULL,
        [NPWP] nvarchar(255) NOT NULL,
        [Address] nvarchar(200) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [Province] nvarchar(max) NOT NULL,
        [PostalCode] int NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Comment] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY ([VendorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoTypes] (
        [WoTypeId] nvarchar(450) NOT NULL,
        [TypeName] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_WoTypes] PRIMARY KEY ([WoTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [UserRole] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [Id] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRole_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRole_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WorkOrders] (
        [WorkOrderId] nvarchar(450) NOT NULL,
        [WoNum] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Note] nvarchar(1000) NULL,
        [ProcurementType] nvarchar(max) NOT NULL,
        [WoNumLetter] nvarchar(max) NULL,
        [DateLetter] datetime2 NULL,
        [From] nvarchar(max) NULL,
        [To] nvarchar(max) NULL,
        [WorkOrderLetter] nvarchar(max) NULL,
        [WBS] nvarchar(max) NULL,
        [GlAccount] nvarchar(max) NULL,
        [DateRequired] datetime2 NULL,
        [XS1] nvarchar(max) NULL,
        [XS2] nvarchar(max) NULL,
        [XS3] nvarchar(max) NULL,
        [XS4] nvarchar(max) NULL,
        [FileWorkOrder] nvarchar(max) NULL,
        [Requester] nvarchar(max) NULL,
        [Approved] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [WoTypeId] nvarchar(450) NULL,
        [StatusId] int NOT NULL,
        [UserId] nvarchar(450) NULL,
        CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([WorkOrderId]),
        CONSTRAINT [AK_WorkOrders_WoNum] UNIQUE ([WoNum]),
        CONSTRAINT [FK_WorkOrders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_WorkOrders_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([StatusId]),
        CONSTRAINT [FK_WorkOrders_WoTypes_WoTypeId] FOREIGN KEY ([WoTypeId]) REFERENCES [WoTypes] ([WoTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoTypesDocuments] (
        [WoTypeDocumentId] nvarchar(450) NOT NULL,
        [IsMandatory] bit NOT NULL,
        [Sequence] int NOT NULL,
        [IsGenerated] bit NOT NULL,
        [IsUploadRequired] bit NOT NULL,
        [RequiresApproval] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        [WoTypeId] nvarchar(450) NOT NULL,
        [DocumentTypeId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_WoTypesDocuments] PRIMARY KEY ([WoTypeDocumentId]),
        CONSTRAINT [FK_WoTypesDocuments_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([DocumentTypeId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WoTypesDocuments_WoTypes_WoTypeId] FOREIGN KEY ([WoTypeId]) REFERENCES [WoTypes] ([WoTypeId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [ProfitLosses] (
        [ProfitLossId] nvarchar(450) NOT NULL,
        [SelectedVendorFinalOffer] decimal(18,2) NOT NULL,
        [Profit] decimal(18,2) NOT NULL,
        [ProfitPercent] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        [SelectedVendorId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_ProfitLosses] PRIMARY KEY ([ProfitLossId]),
        CONSTRAINT [FK_ProfitLosses_Vendors_SelectedVendorId] FOREIGN KEY ([SelectedVendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProfitLosses_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoDetails] (
        [WoDetailId] nvarchar(450) NOT NULL,
        [ItemName] nvarchar(255) NULL,
        [Quantity] int NULL,
        [Unit] nvarchar(max) NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_WoDetails] PRIMARY KEY ([WoDetailId]),
        CONSTRAINT [FK_WoDetails_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoDocuments] (
        [WoDocumentId] nvarchar(450) NOT NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        [DocumentTypeId] nvarchar(450) NOT NULL,
        [FileName] nvarchar(300) NOT NULL,
        [ObjectKey] nvarchar(600) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [Size] bigint NOT NULL,
        [Status] nvarchar(16) NOT NULL DEFAULT N'Uploaded',
        [QrText] nvarchar(512) NULL,
        [QrObjectKey] nvarchar(600) NULL,
        [Description] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [IsApproved] bit NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_WoDocuments] PRIMARY KEY ([WoDocumentId]),
        CONSTRAINT [FK_WoDocuments_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([DocumentTypeId]),
        CONSTRAINT [FK_WoDocuments_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoOffers] (
        [WoOfferId] nvarchar(450) NOT NULL,
        [ItemPenawaran] nvarchar(max) NOT NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_WoOffers] PRIMARY KEY ([WoOfferId]),
        CONSTRAINT [FK_WoOffers_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [DocumentApprovals] (
        [DocumentApprovalId] nvarchar(450) NOT NULL,
        [Level] int NOT NULL,
        [SequenceOrder] int NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [WoTypeDocumentId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_DocumentApprovals] PRIMARY KEY ([DocumentApprovalId]),
        CONSTRAINT [FK_DocumentApprovals_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentApprovals_WoTypesDocuments_WoTypeDocumentId] FOREIGN KEY ([WoTypeDocumentId]) REFERENCES [WoTypesDocuments] ([WoTypeDocumentId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [WoDocumentApprovals] (
        [WoDocumentApprovalId] nvarchar(450) NOT NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        [WoDocumentId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        [ApproverId] nvarchar(450) NULL,
        [Level] int NOT NULL,
        [SequenceOrder] int NOT NULL,
        [Status] nvarchar(16) NOT NULL DEFAULT N'Pending',
        [ApprovedAt] datetime2 NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_WoDocumentApprovals] PRIMARY KEY ([WoDocumentApprovalId]),
        CONSTRAINT [FK_WoDocumentApprovals_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]),
        CONSTRAINT [FK_WoDocumentApprovals_AspNetUsers_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_WoDocumentApprovals_WoDocuments_WoDocumentId] FOREIGN KEY ([WoDocumentId]) REFERENCES [WoDocuments] ([WoDocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WoDocumentApprovals_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [ProfitLossItems] (
        [ProfitLossItemId] nvarchar(450) NOT NULL,
        [TarifAwal] decimal(18,2) NOT NULL,
        [TarifAdd] decimal(18,2) NOT NULL,
        [KmPer25] int NOT NULL,
        [OperatorCost] decimal(18,2) NOT NULL,
        [Revenue] decimal(18,2) NOT NULL,
        [ProfitLossId] nvarchar(450) NOT NULL,
        [WoOfferId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_ProfitLossItems] PRIMARY KEY ([ProfitLossItemId]),
        CONSTRAINT [FK_ProfitLossItems_ProfitLosses_ProfitLossId] FOREIGN KEY ([ProfitLossId]) REFERENCES [ProfitLosses] ([ProfitLossId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProfitLossItems_WoOffers_WoOfferId] FOREIGN KEY ([WoOfferId]) REFERENCES [WoOffers] ([WoOfferId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE TABLE [VendorOffers] (
        [VendorOfferId] nvarchar(450) NOT NULL,
        [Round] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [NoLetter] nvarchar(128) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [WorkOrderId] nvarchar(450) NOT NULL,
        [WoOfferId] nvarchar(450) NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_VendorOffers] PRIMARY KEY ([VendorOfferId]),
        CONSTRAINT [FK_VendorOffers_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE CASCADE,
        CONSTRAINT [FK_VendorOffers_WoOffers_WoOfferId] FOREIGN KEY ([WoOfferId]) REFERENCES [WoOffers] ([WoOfferId]) ON DELETE CASCADE,
        CONSTRAINT [FK_VendorOffers_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([WorkOrderId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_DocumentApprovals_RoleId] ON [DocumentApprovals] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_DocumentApprovals_WoTypeDocumentId] ON [DocumentApprovals] ([WoTypeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_ProfitLosses_SelectedVendorId] ON [ProfitLosses] ([SelectedVendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_ProfitLosses_WorkOrderId] ON [ProfitLosses] ([WorkOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_ProfitLossItems_ProfitLossId] ON [ProfitLossItems] ([ProfitLossId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_ProfitLossItems_WoOfferId] ON [ProfitLossItems] ([WoOfferId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_UserRole_RoleId] ON [UserRole] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_VendorOffers_VendorId] ON [VendorOffers] ([VendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_VendorOffers_WoOfferId] ON [VendorOffers] ([WoOfferId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_VendorOffers_WorkOrderId] ON [VendorOffers] ([WorkOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vendors_VendorCode] ON [Vendors] ([VendorCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDetails_WorkOrderId] ON [WoDetails] ([WorkOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocApprovals_Role_Status] ON [WoDocumentApprovals] ([RoleId], [Status]) INCLUDE ([WoDocumentId], [WorkOrderId], [Level], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocumentApprovals_ApproverId] ON [WoDocumentApprovals] ([ApproverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocumentApprovals_WorkOrderId] ON [WoDocumentApprovals] ([WorkOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WoDocApprovals_Doc_Level_Seq] ON [WoDocumentApprovals] ([WoDocumentId], [Level], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocuments_DocumentTypeId] ON [WoDocuments] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocuments_QrText] ON [WoDocuments] ([QrText]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoDocuments_WorkOrderId_CreatedAt] ON [WoDocuments] ([WorkOrderId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WoDocuments_WorkOrderId_DocumentTypeId_Status] ON [WoDocuments] ([WorkOrderId], [DocumentTypeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoOffers_WorkOrderId] ON [WoOffers] ([WorkOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_StatusId] ON [WorkOrders] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_UserId_CreatedAt_Covering] ON [WorkOrders] ([UserId], [CreatedAt] DESC) INCLUDE ([WoNum], [Description], [StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_WoTypeId] ON [WorkOrders] ([WoTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoTypesDocuments_DocumentTypeId] ON [WoTypesDocuments] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    CREATE INDEX [IX_WoTypesDocuments_WoTypeId] ON [WoTypesDocuments] ([WoTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112221509_1.36.0-alpha'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112221509_1.36.0-alpha', N'9.0.9');
END;

COMMIT;
GO

