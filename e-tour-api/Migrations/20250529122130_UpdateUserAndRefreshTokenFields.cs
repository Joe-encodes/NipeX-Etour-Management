using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_tour_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndRefreshTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "EmailVerificationExpiry", "EmailVerificationToken", "IsEmailVerified", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 12, 21, 28, 593, DateTimeKind.Utc).AddTicks(1750), null, null, false, "$2a$11$SATqkbVb/kXtAGqGDidt2O9MmElJtXh0IUtAo6KRI8mdUDnZK8u/y" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "EmailVerificationExpiry", "EmailVerificationToken", "IsEmailVerified", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 12, 21, 28, 798, DateTimeKind.Utc).AddTicks(5320), null, null, false, "$2a$11$MGUlznxqc06j9yWWyaJd7OQefvAdOusWZWGdDG9D5IwEzBm6RkhMK" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "EmailVerificationExpiry", "EmailVerificationToken", "IsEmailVerified", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 12, 21, 29, 4, DateTimeKind.Utc).AddTicks(6500), null, null, false, "$2a$11$nRbQ19C5elJmM4QjpHz3Je2oapF3iMDkqlyxdrXYx43/BtxX9xc2q" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 8, 24, 7, 958, DateTimeKind.Utc).AddTicks(9030), "$2a$11$U0j9S8sRsc4OPnXb7YxIouZc6ZYyl4gXX/PU0r4LYVEwanQI8Xv/y" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 8, 24, 8, 186, DateTimeKind.Utc).AddTicks(4060), "$2a$11$vYGgzRWZF2sFEFumXCtsUOibIWAWjJpmlpKdjTkqQBI2HGvu1GHJu" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 29, 8, 24, 8, 571, DateTimeKind.Utc).AddTicks(2870), "$2a$11$U3axlLBEbhRFPLArEayTFuubNYFvSVdiqBGl6cDgKZPExmiNoycAi" });
        }
    }
}
