
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Password')
BEGIN
    ALTER TABLE [Users] ALTER COLUMN [Password] NVARCHAR(100) NULL;
END
