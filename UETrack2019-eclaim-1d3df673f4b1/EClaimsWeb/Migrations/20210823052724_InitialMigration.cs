using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EClaimsWeb.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MstClaimType",
                columns: table => new
                {
                    ClaimTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstClaimType", x => x.ClaimTypeID);
                });

            migrationBuilder.CreateTable(
                name: "MstCostStructure",
                columns: table => new
                {
                    CostStructureID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CostStructure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstCostStructure", x => x.CostStructureID);
                });

            migrationBuilder.CreateTable(
                name: "MstCostType",
                columns: table => new
                {
                    CostTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CostType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstCostType", x => x.CostTypeID);
                });

            migrationBuilder.CreateTable(
                name: "MstDepartment",
                columns: table => new
                {
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstDepartment", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "MstRole",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstRole", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "MstScreens",
                columns: table => new
                {
                    ScreenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ScreenName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstScreens", x => x.ScreenID);
                });

            migrationBuilder.CreateTable(
                name: "MstExpenseCategory",
                columns: table => new
                {
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Default = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpenseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsGSTRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimTypeID = table.Column<int>(type: "int", nullable: false),
                    CostTypeID = table.Column<int>(type: "int", nullable: false),
                    CostStructureID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstExpenseCategory", x => x.ExpenseCategoryID);
                    table.ForeignKey(
                        name: "FK_MstExpenseCategory_MstClaimType_ClaimTypeID",
                        column: x => x.ClaimTypeID,
                        principalTable: "MstClaimType",
                        principalColumn: "ClaimTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MstExpenseCategory_MstCostStructure_CostStructureID",
                        column: x => x.CostStructureID,
                        principalTable: "MstCostStructure",
                        principalColumn: "CostStructureID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MstExpenseCategory_MstCostType_CostTypeID",
                        column: x => x.CostTypeID,
                        principalTable: "MstCostType",
                        principalColumn: "CostTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstFacility",
                columns: table => new
                {
                    FacilityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FacilityName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstFacility", x => x.FacilityID);
                    table.ForeignKey(
                        name: "FK_MstFacility_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MstApprovalMatrix",
                columns: table => new
                {
                    AMID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    VerificationLevels = table.Column<int>(type: "int", nullable: false),
                    ApprovalLevels = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ScreenID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstApprovalMatrix", x => x.AMID);
                    table.ForeignKey(
                        name: "FK_MstApprovalMatrix_MstScreens_ScreenID",
                        column: x => x.ScreenID,
                        principalTable: "MstScreens",
                        principalColumn: "ScreenID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstUser",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    AuthenticationSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailConfirmationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmployeeNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsHOD = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsLockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsPhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: false),
                    LockoutEndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedEmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstUser", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_MstUser_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DtApprovalMatrix",
                columns: table => new
                {
                    DTAMID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Verifier = table.Column<int>(type: "int", nullable: false),
                    Approver = table.Column<int>(type: "int", nullable: false),
                    AmountFrom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AmountTo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    AMID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtApprovalMatrix", x => x.DTAMID);
                    table.ForeignKey(
                        name: "FK_DtApprovalMatrix_MstApprovalMatrix_AMID",
                        column: x => x.AMID,
                        principalTable: "MstApprovalMatrix",
                        principalColumn: "AMID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtUserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtUserRoles", x => x.UserRoleID);
                    table.ForeignKey(
                        name: "FK_DtUserRoles_MstRole_RoleID",
                        column: x => x.RoleID,
                        principalTable: "MstRole",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DtUserRoles_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstExpenseClaim",
                columns: table => new
                {
                    ECID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ECNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ClaimTypeID = table.Column<int>(type: "int", nullable: true),
                    Verifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<float>(type: "real", nullable: false),
                    TotalAmount = table.Column<float>(type: "real", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    TnC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstExpenseClaim", x => x.ECID);
                    table.ForeignKey(
                        name: "FK_MstExpenseClaim_MstClaimType_ClaimTypeID",
                        column: x => x.ClaimTypeID,
                        principalTable: "MstClaimType",
                        principalColumn: "ClaimTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstExpenseClaim_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstExpenseClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstExpenseClaim_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MstMileageClaim",
                columns: table => new
                {
                    MCID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MCNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    TravelMode = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Verifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<float>(type: "real", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    TnC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstMileageClaim", x => x.MCID);
                    table.ForeignKey(
                        name: "FK_MstMileageClaim_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstMileageClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstMileageClaim_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MstPVCClaim",
                columns: table => new
                {
                    PVCCID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVCCNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Verifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<float>(type: "real", nullable: false),
                    TotalAmount = table.Column<float>(type: "real", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    TnC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstPVCClaim", x => x.PVCCID);
                    table.ForeignKey(
                        name: "FK_MstPVCClaim_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstPVCClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstPVCClaim_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MstPVGClaim",
                columns: table => new
                {
                    PVGCID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVGCNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Verifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<float>(type: "real", nullable: false),
                    TotalAmount = table.Column<float>(type: "real", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    TnC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstPVGClaim", x => x.PVGCID);
                    table.ForeignKey(
                        name: "FK_MstPVGClaim_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstPVGClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstPVGClaim_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MstTBClaim",
                columns: table => new
                {
                    TBCID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Verifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinalApprover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<float>(type: "real", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalBy = table.Column<int>(type: "int", nullable: false),
                    TnC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstTBClaim", x => x.TBCID);
                    table.ForeignKey(
                        name: "FK_MstTBClaim_MstDepartment_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "MstDepartment",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstTBClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MstTBClaim_MstUser_UserID",
                        column: x => x.UserID,
                        principalTable: "MstUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DtExpenseClaim",
                columns: table => new
                {
                    ECItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ECID = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    GST = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtExpenseClaim", x => x.ECItemID);
                    table.ForeignKey(
                        name: "FK_DtExpenseClaim_MstExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "MstExpenseCategory",
                        principalColumn: "ExpenseCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DtExpenseClaim_MstExpenseClaim_ECID",
                        column: x => x.ECID,
                        principalTable: "MstExpenseClaim",
                        principalColumn: "ECID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtExpenseClaimFileUpload",
                columns: table => new
                {
                    FileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ECID = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtExpenseClaimFileUpload", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_DtExpenseClaimFileUpload_MstExpenseClaim_ECID",
                        column: x => x.ECID,
                        principalTable: "MstExpenseClaim",
                        principalColumn: "ECID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstExpenseClaimAudit",
                columns: table => new
                {
                    AuditID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ECID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    InstanceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstExpenseClaimAudit", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_MstExpenseClaimAudit_MstExpenseClaim_ECID",
                        column: x => x.ECID,
                        principalTable: "MstExpenseClaim",
                        principalColumn: "ECID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtMileageClaim",
                columns: table => new
                {
                    MCItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MCID = table.Column<long>(type: "bigint", nullable: false),
                    DateOfJourney = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    FromFacilityID = table.Column<int>(type: "int", nullable: false),
                    ToFacilityID = table.Column<int>(type: "int", nullable: false),
                    InTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OutTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartReading = table.Column<float>(type: "real", nullable: false),
                    EndReading = table.Column<float>(type: "real", nullable: false),
                    Kms = table.Column<float>(type: "real", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtMileageClaim", x => x.MCItemID);
                    table.ForeignKey(
                        name: "FK_DtMileageClaim_MstFacility_FacilityID",
                        column: x => x.FacilityID,
                        principalTable: "MstFacility",
                        principalColumn: "FacilityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DtMileageClaim_MstMileageClaim_MCID",
                        column: x => x.MCID,
                        principalTable: "MstMileageClaim",
                        principalColumn: "MCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtMileageClaimFileUpload",
                columns: table => new
                {
                    FileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MCID = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtMileageClaimFileUpload", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_DtMileageClaimFileUpload_MstMileageClaim_MCID",
                        column: x => x.MCID,
                        principalTable: "MstMileageClaim",
                        principalColumn: "MCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtTBClaim",
                columns: table => new
                {
                    TBCItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCID = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtTBClaim", x => x.TBCItemID);
                    table.ForeignKey(
                        name: "FK_DtTBClaim_MstExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "MstExpenseCategory",
                        principalColumn: "ExpenseCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DtTBClaim_MstMileageClaim_TBCID",
                        column: x => x.TBCID,
                        principalTable: "MstMileageClaim",
                        principalColumn: "MCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstMileageClaimAudit",
                columns: table => new
                {
                    AuditID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MCID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    InstanceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstMileageClaimAudit", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_MstMileageClaimAudit_MstMileageClaim_MCID",
                        column: x => x.MCID,
                        principalTable: "MstMileageClaim",
                        principalColumn: "MCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtPVCClaim",
                columns: table => new
                {
                    PVCCItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVCCID = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: true),
                    ChequeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Particulars = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Payee = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    GST = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtPVCClaim", x => x.PVCCItemID);
                    table.ForeignKey(
                        name: "FK_DtPVCClaim_MstExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "MstExpenseCategory",
                        principalColumn: "ExpenseCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DtPVCClaim_MstPVCClaim_PVCCID",
                        column: x => x.PVCCID,
                        principalTable: "MstPVCClaim",
                        principalColumn: "PVCCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtPVCClaimFileUpload",
                columns: table => new
                {
                    FileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVCCID = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtPVCClaimFileUpload", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_DtPVCClaimFileUpload_MstPVCClaim_PVCCID",
                        column: x => x.PVCCID,
                        principalTable: "MstPVCClaim",
                        principalColumn: "PVCCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstPVCClaimAudit",
                columns: table => new
                {
                    AuditID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    InstanceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstPVCClaimAudit", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_MstPVCClaimAudit_MstPVCClaim_TBCID",
                        column: x => x.TBCID,
                        principalTable: "MstPVCClaim",
                        principalColumn: "PVCCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtPVGClaim",
                columns: table => new
                {
                    PVGCItemID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVGCID = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: true),
                    ChequeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Particulars = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Payee = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    GST = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtPVGClaim", x => x.PVGCItemID);
                    table.ForeignKey(
                        name: "FK_DtPVGClaim_MstExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "MstExpenseCategory",
                        principalColumn: "ExpenseCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DtPVGClaim_MstPVGClaim_PVGCID",
                        column: x => x.PVGCID,
                        principalTable: "MstPVGClaim",
                        principalColumn: "PVGCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtPVGClaimFileUpload",
                columns: table => new
                {
                    FileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PVGCID = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtPVGClaimFileUpload", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_DtPVGClaimFileUpload_MstPVGClaim_PVGCID",
                        column: x => x.PVGCID,
                        principalTable: "MstPVGClaim",
                        principalColumn: "PVGCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstPVGClaimAudit",
                columns: table => new
                {
                    AuditID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    InstanceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstPVGClaimAudit", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_MstPVGClaimAudit_MstPVGClaim_TBCID",
                        column: x => x.TBCID,
                        principalTable: "MstPVGClaim",
                        principalColumn: "PVGCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DtTBClaimFileUpload",
                columns: table => new
                {
                    FileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCID = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtTBClaimFileUpload", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_DtTBClaimFileUpload_MstTBClaim_TBCID",
                        column: x => x.TBCID,
                        principalTable: "MstTBClaim",
                        principalColumn: "TBCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MstTBClaimAudit",
                columns: table => new
                {
                    AuditID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBCID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    InstanceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MstTBClaimAudit", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_MstTBClaimAudit_MstTBClaim_TBCID",
                        column: x => x.TBCID,
                        principalTable: "MstTBClaim",
                        principalColumn: "TBCID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DtApprovalMatrix_AMID",
                table: "DtApprovalMatrix",
                column: "AMID");

            migrationBuilder.CreateIndex(
                name: "IX_DtExpenseClaim_ECID",
                table: "DtExpenseClaim",
                column: "ECID");

            migrationBuilder.CreateIndex(
                name: "IX_DtExpenseClaim_ExpenseCategoryID",
                table: "DtExpenseClaim",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_DtExpenseClaimFileUpload_ECID",
                table: "DtExpenseClaimFileUpload",
                column: "ECID");

            migrationBuilder.CreateIndex(
                name: "IX_DtMileageClaim_FacilityID",
                table: "DtMileageClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_DtMileageClaim_MCID",
                table: "DtMileageClaim",
                column: "MCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtMileageClaimFileUpload_MCID",
                table: "DtMileageClaimFileUpload",
                column: "MCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVCClaim_ExpenseCategoryID",
                table: "DtPVCClaim",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVCClaim_PVCCID",
                table: "DtPVCClaim",
                column: "PVCCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVCClaimFileUpload_PVCCID",
                table: "DtPVCClaimFileUpload",
                column: "PVCCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVGClaim_ExpenseCategoryID",
                table: "DtPVGClaim",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVGClaim_PVGCID",
                table: "DtPVGClaim",
                column: "PVGCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtPVGClaimFileUpload_PVGCID",
                table: "DtPVGClaimFileUpload",
                column: "PVGCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtTBClaim_ExpenseCategoryID",
                table: "DtTBClaim",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_DtTBClaim_TBCID",
                table: "DtTBClaim",
                column: "TBCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtTBClaimFileUpload_TBCID",
                table: "DtTBClaimFileUpload",
                column: "TBCID");

            migrationBuilder.CreateIndex(
                name: "IX_DtUserRoles_RoleID",
                table: "DtUserRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_DtUserRoles_UserID",
                table: "DtUserRoles",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstApprovalMatrix_ScreenID",
                table: "MstApprovalMatrix",
                column: "ScreenID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseCategory_ClaimTypeID",
                table: "MstExpenseCategory",
                column: "ClaimTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseCategory_CostStructureID",
                table: "MstExpenseCategory",
                column: "CostStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseCategory_CostTypeID",
                table: "MstExpenseCategory",
                column: "CostTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseClaim_ClaimTypeID",
                table: "MstExpenseClaim",
                column: "ClaimTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseClaim_DepartmentID",
                table: "MstExpenseClaim",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseClaim_FacilityID",
                table: "MstExpenseClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseClaim_UserID",
                table: "MstExpenseClaim",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstExpenseClaimAudit_ECID",
                table: "MstExpenseClaimAudit",
                column: "ECID");

            migrationBuilder.CreateIndex(
                name: "IX_MstFacility_DepartmentID",
                table: "MstFacility",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstMileageClaim_DepartmentID",
                table: "MstMileageClaim",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstMileageClaim_FacilityID",
                table: "MstMileageClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_MstMileageClaim_UserID",
                table: "MstMileageClaim",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstMileageClaimAudit_MCID",
                table: "MstMileageClaimAudit",
                column: "MCID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVCClaim_DepartmentID",
                table: "MstPVCClaim",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVCClaim_FacilityID",
                table: "MstPVCClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVCClaim_UserID",
                table: "MstPVCClaim",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVCClaimAudit_TBCID",
                table: "MstPVCClaimAudit",
                column: "TBCID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVGClaim_DepartmentID",
                table: "MstPVGClaim",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVGClaim_FacilityID",
                table: "MstPVGClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVGClaim_UserID",
                table: "MstPVGClaim",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstPVGClaimAudit_TBCID",
                table: "MstPVGClaimAudit",
                column: "TBCID");

            migrationBuilder.CreateIndex(
                name: "IX_MstTBClaim_DepartmentID",
                table: "MstTBClaim",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MstTBClaim_FacilityID",
                table: "MstTBClaim",
                column: "FacilityID");

            migrationBuilder.CreateIndex(
                name: "IX_MstTBClaim_UserID",
                table: "MstTBClaim",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MstTBClaimAudit_TBCID",
                table: "MstTBClaimAudit",
                column: "TBCID");

            migrationBuilder.CreateIndex(
                name: "IX_MstUser_FacilityID",
                table: "MstUser",
                column: "FacilityID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DtApprovalMatrix");

            migrationBuilder.DropTable(
                name: "DtExpenseClaim");

            migrationBuilder.DropTable(
                name: "DtExpenseClaimFileUpload");

            migrationBuilder.DropTable(
                name: "DtMileageClaim");

            migrationBuilder.DropTable(
                name: "DtMileageClaimFileUpload");

            migrationBuilder.DropTable(
                name: "DtPVCClaim");

            migrationBuilder.DropTable(
                name: "DtPVCClaimFileUpload");

            migrationBuilder.DropTable(
                name: "DtPVGClaim");

            migrationBuilder.DropTable(
                name: "DtPVGClaimFileUpload");

            migrationBuilder.DropTable(
                name: "DtTBClaim");

            migrationBuilder.DropTable(
                name: "DtTBClaimFileUpload");

            migrationBuilder.DropTable(
                name: "DtUserRoles");

            migrationBuilder.DropTable(
                name: "MstExpenseClaimAudit");

            migrationBuilder.DropTable(
                name: "MstMileageClaimAudit");

            migrationBuilder.DropTable(
                name: "MstPVCClaimAudit");

            migrationBuilder.DropTable(
                name: "MstPVGClaimAudit");

            migrationBuilder.DropTable(
                name: "MstTBClaimAudit");

            migrationBuilder.DropTable(
                name: "MstApprovalMatrix");

            migrationBuilder.DropTable(
                name: "MstExpenseCategory");

            migrationBuilder.DropTable(
                name: "MstRole");

            migrationBuilder.DropTable(
                name: "MstExpenseClaim");

            migrationBuilder.DropTable(
                name: "MstMileageClaim");

            migrationBuilder.DropTable(
                name: "MstPVCClaim");

            migrationBuilder.DropTable(
                name: "MstPVGClaim");

            migrationBuilder.DropTable(
                name: "MstTBClaim");

            migrationBuilder.DropTable(
                name: "MstScreens");

            migrationBuilder.DropTable(
                name: "MstCostStructure");

            migrationBuilder.DropTable(
                name: "MstCostType");

            migrationBuilder.DropTable(
                name: "MstClaimType");

            migrationBuilder.DropTable(
                name: "MstUser");

            migrationBuilder.DropTable(
                name: "MstFacility");

            migrationBuilder.DropTable(
                name: "MstDepartment");
        }
    }
}
