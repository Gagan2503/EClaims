using Microsoft.EntityFrameworkCore.Migrations;

namespace EClaimsWeb.Migrations
{
    public partial class Initial1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MstApprovalMatrix_MstScreens_ScreenID",
                table: "MstApprovalMatrix");

            migrationBuilder.AlterColumn<int>(
                name: "ScreenID",
                table: "MstApprovalMatrix",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_MstApprovalMatrix_MstScreens_ScreenID",
                table: "MstApprovalMatrix",
                column: "ScreenID",
                principalTable: "MstScreens",
                principalColumn: "ScreenID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MstApprovalMatrix_MstScreens_ScreenID",
                table: "MstApprovalMatrix");

            migrationBuilder.AlterColumn<int>(
                name: "ScreenID",
                table: "MstApprovalMatrix",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MstApprovalMatrix_MstScreens_ScreenID",
                table: "MstApprovalMatrix",
                column: "ScreenID",
                principalTable: "MstScreens",
                principalColumn: "ScreenID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
