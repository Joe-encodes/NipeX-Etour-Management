// 20250324224715_RemoveUserSeeding.cs

using Microsoft.EntityFrameworkCore.Migrations;

namespace e_tour_api.Migrations
{
    public partial class RemoveUserSeeding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded data for the Users table
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-insert the seeded data for the Users table
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, "testuser@example.com", "$2a$11$...", "User", "testuser" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
                values: new object[] { 2, "approver@example.com", "$2a$11$...", "Approver", "approver" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
                values: new object[] { 3, "admin@example.com", "$2a$11$...", "Admin", "admin" });
        }
    }
}