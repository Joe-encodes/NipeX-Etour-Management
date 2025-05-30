using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_tour_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "RefreshTokens");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 28, 14, 23, 36, 29, DateTimeKind.Utc).AddTicks(8890), "$2a$11$.y6bqogJJXxPuSdVQ1YVBet2Azny.QElSsCg13n6.RIKrGOLoqCd6" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 28, 14, 23, 36, 234, DateTimeKind.Utc).AddTicks(9690), "$2a$11$YsAsOUd2yInnrnTjx01Ac.xQFJHq7gZA8R27t4hKOXfI4himwXsKW" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 5, 28, 14, 23, 36, 440, DateTimeKind.Utc).AddTicks(5600), "$2a$11$Bz3E7Lmm33D3epoxdmyQ8OGwLLSRPC/UenZP9eKVuxC4qLG0B.Yw2" });
        }
    }
}
