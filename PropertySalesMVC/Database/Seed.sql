/*
    PropertySalesMVC — minimal starter data for db_acc7ad_ronakestatedb.
    Idempotent: only inserts if the table is empty. Values are placeholders —
    edit AdminLoginDetails' password and AdminDetails/AdminMaster contact info
    before going live.
*/

IF NOT EXISTS (SELECT 1 FROM LocationMaster)
BEGIN
    -- SortOrder here matches the business-required display order (not
    -- alphabetical); Schema.sql also backfills/enforces this by name on
    -- every run, so it's self-healing even if this only partially applies.
    INSERT INTO LocationMaster (Location, IsActive, SortOrder) VALUES
    ('Borivali', 1, 1),
    ('Kandivali', 1, 2),
    ('Malad', 1, 3),
    ('Goregaon', 1, 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM AdminMaster)
BEGIN
    INSERT INTO AdminMaster (AdminName, Phone, WhatsApp, Email, IsActive, CreatedOn)
    VALUES ('Ronak Estate Admin', '9594774795', '919594774795', 'contact@ronakestate.example', 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM AdminDetails)
BEGIN
    INSERT INTO AdminDetails
        (AdminId, CompanyName, OwnerName, Designation,
         HeadOfficeTitle, HeadOfficeAddress,
         BranchOfficeTitle, BranchOfficeAddress,
         InstagramUrl, FacebookUrl, IsActive, CreatedOn)
    VALUES
        (1, 'Ronak Estate', 'Ronak', 'Founder',
         'Head Office', 'Borivali, Mumbai',
         'Branch Office', 'Kandivali, Mumbai',
         'https://instagram.com/ronakestate', 'https://facebook.com/ronakestate', 1, GETDATE());
END
GO

-- NOTE: AdminLoginDetails now stores a PBKDF2 hash, not plaintext.
-- Seeding a real row requires the app's PasswordHasher; see
-- Database/README or run the app once and use the migration script
-- to hash an initial password instead of inserting plaintext here.
IF NOT EXISTS (SELECT 1 FROM AdminLoginDetails)
BEGIN
    PRINT 'Skipping AdminLoginDetails seed — insert a hashed password via the app or a migration script, not plaintext.';
END
GO
