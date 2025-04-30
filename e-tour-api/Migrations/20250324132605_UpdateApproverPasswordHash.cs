using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_tour_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApproverPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "2a$11$cTSuMrdv1r2r9rvjR6n0c.cfoJyHRNvQstYWWu8bzMDVNcYmAFehq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Passw2a$11$cTSuMrdv1r2r9rvjR6n0c.cfoJyHRNvQstYWWu8bzMDVNcYmAFehqordHash",
                value: "$2a$11$YOUR_APPROVER_PASSWORD_HASH_HERE");
        }
    }
}
