using Microsoft.EntityFrameworkCore.Migrations;

namespace EClaimsWeb.Migrations
{
    public partial class removePhoneNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "MstUser");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "DtUserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RoleID",
                table: "DtUserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserRoleID",
                table: "DtUserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DtUserRoles",
                table: "DtUserRoles",
                column: "UserRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_DtUserRoles_RoleID",
                table: "DtUserRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_DtUserRoles_UserID",
                table: "DtUserRoles",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DtUserRoles",
                table: "DtUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_DtUserRoles_RoleID",
                table: "DtUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_DtUserRoles_UserID",
                table: "DtUserRoles");

            migrationBuilder.DropColumn(
                name: "UserRoleID",
                table: "DtUserRoles");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "MstUser",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "DtUserRoles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "RoleID",
                table: "DtUserRoles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
