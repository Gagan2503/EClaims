using Microsoft.EntityFrameworkCore.Migrations;

namespace EClaimsWeb.Migrations
{
    public partial class AddBankDetailsTable1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_mstBankDetails",
                table: "mstBankDetails");

            migrationBuilder.RenameTable(
                name: "mstBankDetails",
                newName: "MstBankDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MstBankDetails",
                table: "MstBankDetails",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MstBankDetails",
                table: "MstBankDetails");

            migrationBuilder.RenameTable(
                name: "MstBankDetails",
                newName: "mstBankDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mstBankDetails",
                table: "mstBankDetails",
                column: "ID");
        }
    }
}
