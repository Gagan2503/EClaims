USE [EClaims]
GO
SET IDENTITY_INSERT [dbo].[MstRole] ON 
GO
INSERT [dbo].[MstRole] ([RoleID], [RoleName], [IsActive], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [ApprovalDate], [ApprovalStatus], [ApprovalBy], [Reason]) VALUES (1, N'Admin', 1, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 2, 2, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 3, 2, NULL)
GO
INSERT [dbo].[MstRole] ([RoleID], [RoleName], [IsActive], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [ApprovalDate], [ApprovalStatus], [ApprovalBy], [Reason]) VALUES (2, N'HR', 1, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 2, 2, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 3, 2, NULL)
GO
INSERT [dbo].[MstRole] ([RoleID], [RoleName], [IsActive], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [ApprovalDate], [ApprovalStatus], [ApprovalBy], [Reason]) VALUES (3, N'NewUser', 1, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 2, 2, CAST(N'2021-08-08T22:28:58.1100000' AS DateTime2), 3, 2, NULL)
GO
SET IDENTITY_INSERT [dbo].[MstRole] OFF
GO
SET IDENTITY_INSERT [dbo].[MstUser] ON 
GO

INSERT [dbo].[MstUser] ([UserID], [NameIdentifier], [AccessFailedCount], [AuthenticationSource], [ConcurrencyStamp], [CreationTime], [CreatorUserId], [DeleterUserId], [DeletionTime], [EmailAddress], [EmailConfirmationCode], [Phone], [EmployeeNo], [IsHOD], [IsActive], [IsDeleted], [IsEmailConfirmed], [IsLockoutEnabled], [IsPhoneNumberConfirmed], [IsTwoFactorEnabled], [LastModificationTime], [LastModifierUserId], [LockoutEndDateUtc], [Name], [NormalizedEmailAddress], [NormalizedUserName], [Password], [PasswordResetCode], [SecurityStamp], [Surname], [TenantId], [UserName], [FacilityID]) VALUES (5, N'subash.kone@gmail.com', 5, N'cookies', NULL, CAST(N'2021-08-10T12:13:53.9940017' AS DateTime2), 1, 1, CAST(N'2021-08-10T12:13:53.9940048' AS DateTime2), N'subash.kone@gmail.com', NULL, N'9900575858', N'123456', 0, 1, 0, 1, 1, 1, 1, CAST(N'2021-08-10T12:13:53.9940058' AS DateTime2), 1, CAST(N'2021-08-10T12:13:53.9940060' AS DateTime2), N'Ramesh kone', NULL, NULL, N'1234', NULL, NULL, NULL, 0, N'subash.kone@gmail.com', 0)
GO

SET IDENTITY_INSERT [dbo].[MstUser] OFF
GO
