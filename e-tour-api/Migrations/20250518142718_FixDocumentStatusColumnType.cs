using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace e_tour_api.etourapi.Migrations
{
    /// <inheritdoc />
    public partial class FixDocumentStatusColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Documents",
                keyColumn: "Id",
                keyValue: 1,
                column: "FilePath",
                value: "../wwwroot/uploads/3fdb0d6a-b94e-437f-8feb-95ead990f86d.pdf");

            migrationBuilder.UpdateData(
                table: "Documents",
                keyColumn: "Id",
                keyValue: 2,
                column: "FilePath",
                value: "../wwwroot/uploads/1a0649bc-6b35-4cb1-83df-2232a5793c3c.PDF");

            // Removed InsertData for Users to avoid duplicate key errors
            // migrationBuilder.InsertData(
            //     table: "Users",
            //     columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
            //     values: new object[,]
            //     {
            //         { 1, "testuser@example.com", "$2a$11$xSvbaWPoLyCJzJ178mGP/.sMmecXPG.eeEjGRLEj1P0zI.F5iAFzW", "User", "testuser" },
            //         { 2, "approver@example.com", "$2a$11$6MU9z5yN3i7QgeTZoqF8Bu7JussC0kxASimKesjyGAKxyAisA9YMq", "Approver", "approver" },
            //         { 3, "admin@example.com", "$2a$11$DhTruhJZQ0ss2N3PPAiXo.dcDeIohWgUGCCxXPCpCrFD9dhXdBkqy", "Admin", "admin" }
            //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed DeleteData for Users to avoid errors
            // migrationBuilder.DeleteData(
            //     table: "Users",
            //     keyColumn: "Id",
            //     keyValue: 1);

            // migrationBuilder.DeleteData(
            //     table: "Users",
            //     keyColumn: "Id",
            //     keyValue: 2);

            // migrationBuilder.DeleteData(
            //     table: "Users",
            //     keyColumn: "Id",
            //     keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Documents",
                keyColumn: "Id",
                keyValue: 1,
                column: "FilePath",
                value: "path/to/test-document-1.pdf");

            migrationBuilder.UpdateData(
                table: "Documents",
                keyColumn: "Id",
                keyValue: 2,
                column: "FilePath",
                value: "path/to/test-document-2.pdf");
        }
    }
}
