

IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'Admin')
BEGIN
    INSERT INTO [Roles] ([Id], [Description]) VALUES ('Admin', 'Admin');
END
