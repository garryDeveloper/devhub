using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DEVHUB016_RefreshTokenFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: EF generated NOT NULL DEFAULT '00000000-…', which would put every
            // existing token into ONE family — and a single reuse would then revoke every user's
            // session. Added nullable, backfilled, then made NOT NULL instead. Each existing token
            // becomes its own family (family_id = id), which is exactly what it is: nothing was
            // ever rotated before this migration.
            migrationBuilder.AddColumn<Guid>(
                name: "family_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE refresh_tokens SET family_id = id;");

            migrationBuilder.AlterColumn<Guid>(
                name: "family_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // xmin is a PostgreSQL system column that every table already has. Npgsql's SQL
            // generator emits nothing for it; the operation is here only so this migration
            // matches the model snapshot.
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "refresh_tokens",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "family_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "refresh_tokens");
        }
    }
}
