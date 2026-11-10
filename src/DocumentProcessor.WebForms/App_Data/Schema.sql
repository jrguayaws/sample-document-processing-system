-- Creates the Documents table if it is not already there.
-- Column types and lengths deliberately match what EF Core generates for the same
-- entity, so the .NET 10 build of this application can share the database.

IF OBJECT_ID(N'[dbo].[Documents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Documents]
    (
        [Id]               uniqueidentifier NOT NULL,
        [FileName]         nvarchar(260)  NOT NULL,
        [OriginalFileName] nvarchar(260)  NOT NULL,
        [FileExtension]    nvarchar(16)   NOT NULL,
        [FileSize]         bigint         NOT NULL,
        [ContentType]      nvarchar(128)  NOT NULL,
        [StoragePath]      nvarchar(512)  NOT NULL,
        [UploadedAt]       datetimeoffset NOT NULL,
        [Status]           int            NOT NULL,
        [Summary]          nvarchar(max)  NULL,
        [UploadedBy]       nvarchar(128)  NOT NULL,
        [IsDeleted]        bit            NOT NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedAt'
               AND object_id = OBJECT_ID(N'[dbo].[Documents]'))
BEGIN
    CREATE INDEX [IX_Documents_UploadedAt] ON [dbo].[Documents] ([UploadedAt] DESC);
END;
