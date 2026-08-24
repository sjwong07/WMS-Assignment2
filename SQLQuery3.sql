ALTER TABLE [dbo].[Users] 
ADD CONSTRAINT UQ_Users_Username UNIQUE (Username);

ALTER TABLE [dbo].[Users] 
ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);