IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'Member')
BEGIN
    INSERT INTO [Roles] ([Id], [Description]) VALUES ('Member', 'Member');
END

IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 'Admin')
BEGIN
    INSERT INTO [Roles] ([Id], [Description]) VALUES ('Admin', 'Admin');
END
