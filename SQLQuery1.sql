-- 1. Ensure Admin Role exists
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'Admin')
BEGIN
    ALTER TABLE [Users] ALTER COLUMN [Password] NVARCHAR(100) NULL;
END