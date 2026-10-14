using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shelfora.Data;

namespace Shelfora.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20261014000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE [Categories] (
                [Id] int NOT NULL IDENTITY,
                [Name] nvarchar(80) COLLATE Latin1_General_100_CI_AS NOT NULL,
                [Description] nvarchar(500) NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
            CREATE TABLE [LibrarySettings] (
                [Id] int NOT NULL IDENTITY,
                [LibraryName] nvarchar(160) NOT NULL,
                [Address] nvarchar(300) NULL,
                [ContactEmail] nvarchar(254) NULL,
                [ContactNumber] nvarchar(40) NULL,
                [DefaultLoanDays] int NOT NULL,
                [DailyFineRate] decimal(10,2) NOT NULL,
                [Description] nvarchar(1000) NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                CONSTRAINT [PK_LibrarySettings] PRIMARY KEY ([Id])
            );
            CREATE TABLE [Members] (
                [Id] int NOT NULL IDENTITY,
                [MemberCode] nvarchar(30) NOT NULL,
                [FullName] nvarchar(160) NOT NULL,
                [Email] nvarchar(254) NULL,
                [ContactNumber] nvarchar(40) NULL,
                [Address] nvarchar(500) NULL,
                [MembershipDate] datetime2 NOT NULL,
                [IsActive] bit NOT NULL,
                [Notes] nvarchar(1000) NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Members] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Members_MemberCode] ON [Members] ([MemberCode]);
            CREATE INDEX [IX_Members_FullName] ON [Members] ([FullName]);
            CREATE TABLE [Books] (
                [Id] int NOT NULL IDENTITY,
                [BookCode] nvarchar(30) NOT NULL,
                [Isbn] nvarchar(20) NULL,
                [Title] nvarchar(180) NOT NULL,
                [Author] nvarchar(180) NOT NULL,
                [CategoryId] int NOT NULL,
                [Publisher] nvarchar(120) NULL,
                [PublicationYear] int NULL,
                [Description] nvarchar(2000) NULL,
                [CoverImageUrl] nvarchar(500) NULL,
                [ShelfLocation] nvarchar(100) NULL,
                [TotalCopies] int NOT NULL,
                [AvailableCopies] int NOT NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Books] PRIMARY KEY ([Id]),
                CONSTRAINT [CK_Books_CopyCounts] CHECK ([TotalCopies] >= 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]),
                CONSTRAINT [FK_Books_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
            );
            CREATE UNIQUE INDEX [IX_Books_BookCode] ON [Books] ([BookCode]);
            CREATE UNIQUE INDEX [IX_Books_Isbn] ON [Books] ([Isbn]) WHERE [Isbn] IS NOT NULL;
            CREATE INDEX [IX_Books_Title_Author] ON [Books] ([Title], [Author]);
            CREATE INDEX [IX_Books_CategoryId] ON [Books] ([CategoryId]);
            CREATE TABLE [BorrowTransactions] (
                [Id] int NOT NULL IDENTITY,
                [TransactionCode] nvarchar(30) NOT NULL,
                [MemberId] int NOT NULL,
                [BookId] int NOT NULL,
                [Quantity] int NOT NULL,
                [QuantityReturned] int NOT NULL,
                [BorrowDate] datetime2 NOT NULL,
                [DueDate] datetime2 NOT NULL,
                [ReturnedAtUtc] datetime2 NULL,
                [Status] int NOT NULL,
                [Notes] nvarchar(1000) NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_BorrowTransactions] PRIMARY KEY ([Id]),
                CONSTRAINT [CK_BorrowTransactions_Quantities] CHECK ([Quantity] > 0 AND [QuantityReturned] >= 0 AND [QuantityReturned] <= [Quantity]),
                CONSTRAINT [CK_BorrowTransactions_Dates] CHECK ([DueDate] >= [BorrowDate]),
                CONSTRAINT [FK_BorrowTransactions_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE NO ACTION,
                CONSTRAINT [FK_BorrowTransactions_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
            );
            CREATE UNIQUE INDEX [IX_BorrowTransactions_TransactionCode] ON [BorrowTransactions] ([TransactionCode]);
            CREATE INDEX [IX_BorrowTransactions_DueDate_Status] ON [BorrowTransactions] ([DueDate], [Status]);
            CREATE INDEX [IX_BorrowTransactions_BookId] ON [BorrowTransactions] ([BookId]);
            CREATE INDEX [IX_BorrowTransactions_MemberId] ON [BorrowTransactions] ([MemberId]);
            CREATE TABLE [ReturnRecords] (
                [Id] int NOT NULL IDENTITY,
                [BorrowTransactionId] int NOT NULL,
                [SubmissionToken] nvarchar(36) NOT NULL,
                [Quantity] int NOT NULL,
                [ReturnedAtUtc] datetime2 NOT NULL,
                [Notes] nvarchar(500) NULL,
                CONSTRAINT [PK_ReturnRecords] PRIMARY KEY ([Id]),
                CONSTRAINT [CK_ReturnRecords_Quantity] CHECK ([Quantity] > 0),
                CONSTRAINT [FK_ReturnRecords_BorrowTransactions_BorrowTransactionId] FOREIGN KEY ([BorrowTransactionId]) REFERENCES [BorrowTransactions] ([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_ReturnRecords_BorrowTransactionId] ON [ReturnRecords] ([BorrowTransactionId]);
            CREATE UNIQUE INDEX [IX_ReturnRecords_SubmissionToken] ON [ReturnRecords] ([SubmissionToken]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE [ReturnRecords];
            DROP TABLE [BorrowTransactions];
            DROP TABLE [Books];
            DROP TABLE [Members];
            DROP TABLE [LibrarySettings];
            DROP TABLE [Categories];
            """);
    }
}
