using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skillbridge.Migrations
{
    public partial class AddUserIdToSessionAccess : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('SessionAccesses', 'UserId') IS NULL
                BEGIN
                    ALTER TABLE [SessionAccesses]
                    ADD [UserId] nvarchar(450) NOT NULL DEFAULT N'';
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_SessionAccesses_UserId'
                      AND object_id = OBJECT_ID('SessionAccesses')
                )
                BEGIN
                    CREATE INDEX [IX_SessionAccesses_UserId]
                    ON [SessionAccesses] ([UserId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_SessionAccesses_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [SessionAccesses]
                    ADD CONSTRAINT [FK_SessionAccesses_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id])
                    ON DELETE NO ACTION;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_SessionAccesses_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [SessionAccesses]
                    DROP CONSTRAINT [FK_SessionAccesses_AspNetUsers_UserId];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_SessionAccesses_UserId'
                      AND object_id = OBJECT_ID('SessionAccesses')
                )
                BEGIN
                    DROP INDEX [IX_SessionAccesses_UserId] ON [SessionAccesses];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('SessionAccesses', 'UserId') IS NOT NULL
                BEGIN
                    ALTER TABLE [SessionAccesses]
                    DROP COLUMN [UserId];
                END
            ");
        }
    }
}