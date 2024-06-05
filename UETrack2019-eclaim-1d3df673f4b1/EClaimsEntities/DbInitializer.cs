using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities
{
    public static class DbInitializer
    {
        public static void Initialize(RepositoryContext context)
        {
            context.Database.EnsureCreated();
            #region Insert User
            // Look for any users.
            if (context.users.Any())
            {
                return; // DB has been seeded
            }

            var departments = new MstDepartment[]
            {
                new MstDepartment
                {
                    Department = "Department 01",
                    CreatedBy = 1,
                    ApprovalBy = 1,
                    ApprovalDate = DateTime.Now,
                    ApprovalStatus  = 1,
                    Code = "DEP01",
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    ModifiedBy = 1,
                    ModifiedDate = DateTime.Now,
                    Reason = "Department Reason"
                }
            };

            foreach (MstDepartment s in departments)
            {
                context.mstDepartments.Add(s);
            }
            context.SaveChanges();

            var taxclass = new MstTaxClass[]
            {
                new MstTaxClass
                {
                    TaxClass = 40.5m,
                    //CreatedBy = 1,
                    //ApprovalBy = 1,
                    //ApprovalDate = DateTime.Now,
                    //ApprovalStatus  = 1,
                    Code = 1,
                    //CreatedDate = DateTime.Now,
                    IsActive = true
                    //ModifiedBy = 1,
                    //ModifiedDate = DateTime.Now,
                    //Reason = "Taxclass Reason"
                }
            };

            foreach (MstTaxClass s in taxclass)
            {
                context.mstTaxClass.Add(s);
            }
            context.SaveChanges();

            var facilities = new MstFacility[]
            {
                new MstFacility
                {
                    FacilityName = "Facility 01",
                    IsActive = true,
                    Reason = "Facility Reason",
                    Code = "FAC01",
                    ApprovalStatus = 1,
                    ApprovalDate = DateTime.Now,
                    ApprovalBy = 1,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now,
                    DepartmentID = 1,
                    ModifiedBy = 1,
                    ModifiedDate = DateTime.Now
                }
            };

            foreach (MstFacility s in facilities)
            {
                context.mstFacilities.Add(s);
            }
            context.SaveChanges();

            var users = new MstUser[]
            {
                new MstUser
                {
                    NameIdentifier="superadmin",
                    AccessFailedCount = 0,
                    AuthenticationSource = "cookies",
                    ConcurrencyStamp = null,
                    CreationTime = DateTime.Now,
                    CreatorUserId = 0,
                    DeleterUserId = 0,
                    DeletionTime= DateTime.Now,
                    EmailAddress = "superadmin@uemsgroup.com",
                    EmailConfirmationCode = "",
                    Phone = "",
                    EmployeeNo = "EC0001",
                    IsHOD= false,
                    IsActive = true,
                    IsDeleted = false,
                    IsEmailConfirmed = true,
                    IsLockoutEnabled = true,
                    IsPhoneNumberConfirmed = true,
                    IsTwoFactorEnabled = false,
                    LastModificationTime = DateTime.Now,
                    LastModifierUserId = 0,
                    LockoutEndDateUtc = DateTime.Now,
                    Name = "Super Admin",
                    NormalizedEmailAddress = "superadmin@uemsgroup.com",
                    NormalizedUserName = "Super Admin",
                    Password = "1234",
                    PasswordResetCode = "",
                    SecurityStamp = "",
                    Surname = "SA",
                    TenantId = 0,
                    UserName = "superadmin@uemsgroup.com",
                    FacilityID = 1
                }
            };
            foreach (MstUser s in users)
            {
                context.users.Add(s);
            }
            context.SaveChanges();
            #endregion

            #region Create Roles Master
            // Look for any users.
            if (context.mstRoles.Any())
            {
                return; // DB has been seeded
            }
            var roles = new MstRole[]
            {
                new MstRole {
                    RoleName = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = 0,
                    ModifiedBy = 0,
                    ApprovalDate = DateTime.Now,
                    ApprovalStatus = 0,
                    ApprovalBy = 0,
                    Reason = ""
                },
                new MstRole {
                    RoleName = "Finance",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = 0,
                    ModifiedBy = 0,
                    ApprovalDate = DateTime.Now,
                    ApprovalStatus = 0,
                    ApprovalBy = 0,
                    Reason = ""
                },
                new MstRole {
                    RoleName = "HR",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = 0,
                    ModifiedBy = 0,
                    ApprovalDate = DateTime.Now,
                    ApprovalStatus = 0,
                    ApprovalBy = 0,
                    Reason = ""
                },
                new MstRole {
                    RoleName = "User",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = 0,
                    ModifiedBy = 0,
                    ApprovalDate = DateTime.Now,
                    ApprovalStatus = 0,
                    ApprovalBy = 0,
                    Reason = ""
                }
            };
            foreach (MstRole s in roles)
            {
                context.mstRoles.Add(s);
            }
            context.SaveChanges();
            #endregion

            #region Assign User to Role
            // Look for any users.
            if (context.dtUserRoles.Any())
            {
                return; // DB has been seeded
            }

            var dtRoles = new DtUserRoles[]
            {
                new DtUserRoles
                {
                    RoleID = 1,
                    UserID = 1
                }
            };
            foreach (DtUserRoles s in dtRoles)
            {
                context.dtUserRoles.Add(s);
            }
            context.SaveChanges();
            #endregion

            // Need to insert Claim types and claim categories
        }
    }
}
