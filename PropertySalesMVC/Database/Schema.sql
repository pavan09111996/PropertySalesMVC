/*
    PropertySalesMVC — schema for db_acc7ad_ronakestatedb (SQL5063.site4now.net)

    Reverse-engineered from every column referenced by the application's SQL
    queries prior to the repository/service refactor. Run this once against a
    fresh database before pointing the app at it. Safe to re-run (uses
    IF NOT EXISTS guards).
*/

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LocationMaster')
BEGIN
    CREATE TABLE LocationMaster
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Location    NVARCHAR(200)   NOT NULL,
        IsActive    BIT             NOT NULL DEFAULT 1
    );
END
GO

-- Display order for location dropdowns app-wide. Not alphabetical —
-- business wants Borivali, Kandivali, Malad, Goregaon specifically.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LocationMaster') AND name = 'SortOrder')
BEGIN
    ALTER TABLE LocationMaster ADD SortOrder INT NOT NULL DEFAULT 0;
END
GO

-- Backfill/enforce the canonical order for the four seeded locations every
-- run (harmless no-op once already set) — self-healing if ever hand-edited.
UPDATE LocationMaster SET SortOrder = 1 WHERE Location = 'Borivali';
UPDATE LocationMaster SET SortOrder = 2 WHERE Location = 'Kandivali';
UPDATE LocationMaster SET SortOrder = 3 WHERE Location = 'Malad';
UPDATE LocationMaster SET SortOrder = 4 WHERE Location = 'Goregaon';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Properties')
BEGIN
    CREATE TABLE Properties
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Title           NVARCHAR(200)   NOT NULL,
        Location        INT             NOT NULL REFERENCES LocationMaster(Id),
        Price           DECIMAL(18,2)   NOT NULL,
        Description     NVARCHAR(MAX)   NULL,
        LookingFor      INT             NOT NULL,   -- 1 = Rent, 2 = Buy, 3 = Sell
        BHK             INT             NOT NULL,
        VideoPath       NVARCHAR(500)   NULL,
        IsActive        BIT             NOT NULL DEFAULT 1,
        CreatedDate     DATETIME        NOT NULL DEFAULT GETDATE()
    );
END
GO

-- Admin-controlled flag (Add/Edit Property) — surfaces the property in the
-- Home page's auto-scrolling "Featured" panel.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Properties') AND name = 'IsFeatured')
BEGIN
    ALTER TABLE Properties ADD IsFeatured BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PropertyImages')
BEGIN
    CREATE TABLE PropertyImages
    (
        ImageId         INT IDENTITY(1,1) PRIMARY KEY,
        PropertyId      INT             NOT NULL REFERENCES Properties(Id),
        ImagePath       NVARCHAR(500)   NULL
    );
END
GO

-- Images have always been stored as files on the web server disk since the
-- repository/service refactor; ImageBase64 was a legacy in-DB storage path that
-- nothing has written to since. Dropped to save space against the DB's storage budget.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PropertyImages') AND name = 'ImageBase64')
BEGIN
    ALTER TABLE PropertyImages DROP COLUMN ImageBase64;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminDetails')
BEGIN
    CREATE TABLE AdminDetails
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        AdminId             INT             NULL,
        CompanyName         NVARCHAR(200)   NULL,
        OwnerName           NVARCHAR(200)   NULL,
        Designation         NVARCHAR(200)   NULL,
        HeadOfficeTitle     NVARCHAR(200)   NULL,
        HeadOfficeAddress   NVARCHAR(500)   NULL,
        BranchOfficeTitle   NVARCHAR(200)   NULL,
        BranchOfficeAddress NVARCHAR(500)   NULL,
        InstagramUrl        NVARCHAR(300)   NULL,
        FacebookUrl         NVARCHAR(300)   NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedOn           DATETIME        NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminMaster')
BEGIN
    CREATE TABLE AdminMaster
    (
        AdminId     INT IDENTITY(1,1) PRIMARY KEY,
        AdminName   NVARCHAR(200)   NOT NULL,
        Phone       NVARCHAR(20)    NULL,
        WhatsApp    NVARCHAR(20)    NULL,
        Email       NVARCHAR(200)   NULL,
        IsActive    BIT             NOT NULL DEFAULT 1,
        CreatedOn   DATETIME        NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminLoginDetails')
BEGIN
    CREATE TABLE AdminLoginDetails
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        AdminID         NVARCHAR(100)   NOT NULL,
        PasswordHash    NVARCHAR(300)   NOT NULL,
        FailedAttempts  INT             NOT NULL DEFAULT 0,
        LockoutUntil    DATETIME        NULL,
        IsActive        BIT             NOT NULL DEFAULT 1
    );
END
GO

-- Upgrade path for a pre-existing AdminLoginDetails table created before
-- password hashing / lockout support was added.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminLoginDetails') AND name = 'Password')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminLoginDetails') AND name = 'PasswordHash')
BEGIN
    EXEC sp_rename 'AdminLoginDetails.Password', 'PasswordHash', 'COLUMN';
    ALTER TABLE AdminLoginDetails ALTER COLUMN PasswordHash NVARCHAR(300) NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminLoginDetails') AND name = 'FailedAttempts')
BEGIN
    ALTER TABLE AdminLoginDetails ADD FailedAttempts INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminLoginDetails') AND name = 'LockoutUntil')
BEGIN
    ALTER TABLE AdminLoginDetails ADD LockoutUntil DATETIME NULL;
END
GO

-- 'Admin' | 'Developer' — Developer accounts can only reach /Admin/ErrorLogs
-- (see DeveloperAuthorizeAttribute); everything else is Admin-only.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminLoginDetails') AND name = 'Role')
BEGIN
    ALTER TABLE AdminLoginDetails ADD Role NVARCHAR(20) NOT NULL DEFAULT 'Admin';
END
GO

-- Removed 2026-07-28: the business decided WhatsApp itself is sufficient as
-- the record of customer contact — the admin-side lead inbox this fed
-- (/Admin/Enquiries) added a duplicate record nobody was using. Dropped
-- outright (not just stopped writing to) at the user's explicit choice,
-- since the historical lead data was never going to be looked at again.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PropertyEnquiries')
BEGIN
    DROP TABLE PropertyEnquiries;
END
GO

-- Anonymous listing-view analytics — aggregated counters, not per-visit event
-- rows, to stay tiny against the 1000MB DB budget (one row per
-- date+mode+location+BHK combo that ever occurred, not one row per view —
-- growth is capped by combination count, not traffic, so even years of data
-- stay negligible; see ListingViewStatsRepository.RecordViewAsync).
-- No IP/session/identity is ever recorded — this is deliberately
-- non-identifying, since the site never requires a customer to log in.
-- Written from PropertyController.ListingPartial (first page of a
-- search/filter only, not every pagination click) via IListingViewStatsRepository,
-- which also self-purges rows older than 730 days (2 years) on every write —
-- a generous safety net, not a real storage-pressure fix given the math above.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ListingViewStats')
BEGIN
    CREATE TABLE ListingViewStats
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        ViewDate    DATE            NOT NULL,
        Mode        NVARCHAR(10)    NOT NULL,   -- 'Buy' | 'Rent'
        LocationId  INT             NULL REFERENCES LocationMaster(Id),  -- NULL = "All Locations" was selected
        BHK         INT             NULL,                                -- NULL = "Any BHK" was selected
        ViewCount   INT             NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_ListingViewStats_ViewDate ON ListingViewStats (ViewDate);
END
GO

-- Error/exception log, deliberately capped-width columns (no NVARCHAR(MAX)) to
-- bound worst-case row size given the tight 1000MB DB budget. Rows older than
-- 30 days are purged automatically by the app on every write (Logging/DatabaseLogger.cs)
-- — this table should never be allowed to grow unbounded.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ErrorLogs')
BEGIN
    CREATE TABLE ErrorLogs
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Severity        NVARCHAR(20)    NOT NULL,   -- 'Error' | 'Critical'
        Message         NVARCHAR(1000)  NOT NULL,
        ExceptionType   NVARCHAR(300)   NULL,
        StackTrace      NVARCHAR(4000)  NULL,
        Source          NVARCHAR(300)   NULL,       -- logger category, e.g. controller/service name
        RequestPath     NVARCHAR(500)   NULL,
        CreatedOn       DATETIME        NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_ErrorLogs_CreatedOn ON ErrorLogs (CreatedOn);
END
GO
