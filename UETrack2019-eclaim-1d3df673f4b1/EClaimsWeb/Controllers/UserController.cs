using AutoMapper;
using AutoMapper.Configuration;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EClaimsWeb.Helpers;
using OfficeOpenXml;
using System.Security.Claims;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;

        public UserController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var mstUsers = await _repository.MstUser.GetAllUsersAsync();
                _logger.LogInfo($"Returned all users from database.");

                var mstUsersResult = _mapper.Map<IEnumerable<MstUser>>(mstUsers);
                return View(mstUsersResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllUsersAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Create()
        {
            UserVM model = new UserVM();
            model.drpRoles = _repository.MstRole.GetAllRolesAsync().GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.RoleName, Value = x.RoleID.ToString() }).ToList();
            model.drpFacilities = _repository.MstFacility.GetAllFacilityAsync("active").GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.FacilityName, Value = x.FacilityID.ToString() }).ToList();
            //db.Subjects.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();

            //ViewData["RoleID"] = new SelectList(await _repository.MstRole.GetAllRolesAsync(), "RoleID", "RoleName");
            return View(model);
        }

        // Excel Downlaod
        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[8] { new DataColumn("EmployeeNo"),
                                            new DataColumn("Name"),
                                            new DataColumn("Phone"),
                                            new DataColumn("Email"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Role"),
                                            new DataColumn("IsHOD"),
                                            new DataColumn("Active")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeTemplate.xlsx");
                }
            }
            */
            string id = "EmployeeTemplate.xlsx";

            var file = ("~/ExcelTemplates/" + id);
            return File(file, "application/octet-stream", id);
        }

        private IHostingEnvironment _hostingEnv;
        public void HomeController(IHostingEnvironment hostingEnv)
        {
            _hostingEnv = hostingEnv;
        }


        [HttpPost]

        public async Task<IActionResult> ImportExcelFile(IFormFile FormFile)
        {
            try
            {
                var filename = ContentDispositionHeaderValue.Parse(FormFile.ContentDisposition).FileName.Trim('"');

                var MainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");

                if (!Directory.Exists(MainPath))
                {
                    Directory.CreateDirectory(MainPath);
                }

                var filePath = Path.Combine(MainPath, FormFile.FileName);
                using (System.IO.Stream stream = new FileStream(filePath, FileMode.Create))
                {
                    await FormFile.CopyToAsync(stream);
                }

                string extension = Path.GetExtension(filename);
                string Result = Path.GetFileNameWithoutExtension(filename);

                string conString = string.Empty;

                FileInfo fileInfo = new FileInfo(filePath);
                ExcelPackage package = new ExcelPackage(fileInfo);
                //create a new Excel package in a memorystream
                DataTable dt = new DataTable();
                dt = ExcelPackageToDataTable(package);

                DataRow[] drows = dt.Select();

                for (int i = 0; i < drows.Length; i++)
                {
                    dt.Rows[i]["UserName"] = User.FindFirstValue("username");
                    dt.Rows[i]["Userid"] = User.FindFirstValue("userid");
                    dt.Rows[i]["Status"] = "Load";
                    dt.Rows[i].EndEdit();
                    dt.AcceptChanges();
                }

                //conString = @"server=NGBLR-03223; database=EClaims; User ID=sa;password=Nextgen@123";
                SqlCommand cmd = new SqlCommand();
                using (SqlConnection con = new SqlConnection(_context.Connection.ConnectionString))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {
                        cmd = new SqlCommand("delete from MstUserTemp", con);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstUserTemp";

                        sqlBulkCopy.ColumnMappings.Add("EmployeeNo", "EmployeeNo");
                        sqlBulkCopy.ColumnMappings.Add("Name", "[Name]");
                        sqlBulkCopy.ColumnMappings.Add("Phone", "Phone");
                        sqlBulkCopy.ColumnMappings.Add("Email", "Email");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("Role", "Role");
                        sqlBulkCopy.ColumnMappings.Add("IsHOD", "IsHOD");
                        sqlBulkCopy.ColumnMappings.Add("Active", "Active");
                        sqlBulkCopy.ColumnMappings.Add("MileageLimit", "MileageLimit");
                        sqlBulkCopy.ColumnMappings.Add("TelephoneBillLimit", "TelephoneBillLimit");
                        sqlBulkCopy.ColumnMappings.Add("PettyCashFloat", "PettyCashFloatLimit");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstUser.InsertExcel();

                int count = 0;

                if (InvaildData.Rows.Count > 0)
                {
                    count = int.Parse(InvaildData.Rows[0]["Invaild"].ToString());
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                        _toastNotification.AddSuccessToastMessage("File has imported.Please check the downloaded file.", new NotyOptions() { Timeout = 5000 });
                        return RedirectToAction("Index", "User");
                    }
                    else
                    {
                        using (XLWorkbook wb = new XLWorkbook())
                        {
                            wb.Worksheets.Add(InvaildData);
                            using (MemoryStream stream = new MemoryStream())
                            {
                                wb.SaveAs(stream);
                                Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                                _toastNotification.AddSuccessToastMessage($"Import process completed. Please check the downloaded file to verify if the data has been successfully imported");
                                //_toastNotification.AddSuccessToastMessage("File has imported.Please check the downloaded file.", new NotyOptions() { Timeout = 5000 });
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeTemplateValidate.xlsx");
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong while uploading file: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed while uploading file. Error: {ex.Message}");
                return RedirectToAction("Index");
            }
            
            return RedirectToAction("Index", "User");
        }

        public static DataTable ExcelPackageToDataTable(ExcelPackage excelPackage)
        {
            DataTable dt = new DataTable();
            var currentSheet = excelPackage.Workbook.Worksheets;
            ExcelWorksheet worksheet = currentSheet.First();

            //check if the worksheet is completely empty
            if (worksheet.Dimension == null)
            {
                return dt;
            }

            //create a list to hold the column names
            List<string> columnNames = new List<string>();

            //needed to keep track of empty column headers
            int currentColumn = 1;

            //loop all columns in the sheet and add them to the datatable
            foreach (var cell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                string columnName = cell.Text.Trim();

                //check if the previous header was empty and add it if it was
                if (cell.Start.Column != currentColumn)
                {
                    columnNames.Add("Header_" + currentColumn);
                    dt.Columns.Add("Header_" + currentColumn);
                    currentColumn++;
                }

                //add the column name to the list to count the duplicates
                columnNames.Add(columnName);

                //count the duplicate column names and make them unique to avoid the exception
                //A column named 'Name' already belongs to this DataTable
                int occurrences = columnNames.Count(x => x.Equals(columnName));
                if (occurrences > 1)
                {
                    columnName = columnName + "_" + occurrences;
                }
                if (columnName == "Date")
                {
                    DataColumn colDateTime = new DataColumn("Date");
                    colDateTime.DataType = System.Type.GetType("System.DateTime");
                    dt.Columns.Add(colDateTime);
                }
                else
                {
                    //add the column to the datatable
                    dt.Columns.Add(columnName);
                }


                currentColumn++;
            }
            dt.Columns.Add("UserName");
            dt.Columns.Add("Userid");
            dt.Columns.Add("Status");

            //start adding the contents of the excel file to the datatable
            for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
            {
                var row = worksheet.Cells[i, 1, i, worksheet.Dimension.End.Column];
                DataRow newRow = dt.NewRow();

                //loop all cells in the row
                foreach (var cell in row)
                {
                    if (cell.Address.Contains("I") || cell.Address.Contains("J") || cell.Address.Contains("K"))
                    {
                        newRow[cell.Start.Column - 1] = cell.Value;
                    }
                    else
                    {
                        newRow[cell.Start.Column - 1] = cell.Text;
                    }
                }

                dt.Rows.Add(newRow);
            }

            return dt;
        }

        // [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserVM mstUserVM)
        {
            try
            {
                if (mstUserVM == null)
                {
                    _logger.LogError("User object sent from client is null.");
                    return BadRequest("User object is null");
                }
                //var mstUser = _mapper.Map<MstUser>(mstUser1);

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid user object while add. Error = " + modelErrors);
                    //ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync(), "DepartmentID", "Department", mstFacility.DepartmentID);
                    return View(mstUserVM);
                    //return BadRequest("Invalid model object");
                }

                MstUser mstUser = new MstUser();
                List<DtUserRoles> userRoles = new List<DtUserRoles>();
                List<DtUserFacilities> userFacilities = new List<DtUserFacilities>();
                mstUser.Name = mstUserVM.Name;
                mstUser.EmployeeNo = mstUserVM.EmployeeNo;
                mstUser.Phone = mstUserVM.Phone;
                mstUser.EmailAddress = mstUserVM.EmailAddress;
                mstUser.IsActive = mstUserVM.IsActive;
                mstUser.IsHOD = mstUserVM.IsHOD;

                if (_repository.MstUser.ValidateUser(mstUser, "create"))
                {
                    //TempData["Error"] = "User already exists.";
                    //ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync(), "DepartmentID", "Department", mstFacility.DepartmentID);
                    mstUserVM.drpRoles = _repository.MstRole.GetAllRolesAsync().GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.RoleName, Value = x.RoleID.ToString() }).ToList();
                    mstUserVM.drpFacilities = _repository.MstFacility.GetAllFacilityAsync("active").GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.FacilityName, Value = x.FacilityID.ToString() }).ToList();
                    // ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync(), "DepartmentID", "Department", mstFacility.DepartmentID);
                    //return View(mstUserVM);
                    _toastNotification.AddErrorToastMessage("User already exists", new NotyOptions() { Timeout = 5000 });
                    return View(mstUserVM);
                }
                else
                {
                    mstUser.AuthenticationSource = "cookies";
                    mstUser.NameIdentifier = mstUser.EmailAddress;
                    mstUser.UserName = mstUser.EmailAddress;
                    //mstUser.FacilityID = 1;
                    //mstUser.Phone = "MS";
                    mstUser.Password = Aes256CbcEncrypter.Encrypt("1234");
                    mstUser.AccessFailedCount = 5;
                    mstUser.CreationTime = DateTime.Now;
                    mstUser.CreatorUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstUser.DeleterUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstUser.DeletionTime = DateTime.Now;
                    //mstUser.IsActive = true;
                    //mstUser.IsHOD = false;
                    mstUser.IsDeleted = false;
                    mstUser.IsEmailConfirmed = true;
                    mstUser.IsLockoutEnabled = true;
                    mstUser.IsPhoneNumberConfirmed = true;
                    mstUser.IsTwoFactorEnabled = false;
                    mstUser.LastModificationTime = DateTime.Now;
                    mstUser.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstUser.LockoutEndDateUtc = DateTime.Now;
                    //mstUser.FacilityID = mstUserVM.FacilityID;
                    mstUser.ExpenseLimit = mstUserVM.ExpenseLimit;
                    mstUser.MileageLimit = mstUserVM.MileageLimit;
                    mstUser.TelephoneLimit = mstUserVM.TelephoneLimit;

                    if (mstUserVM.RoleIds.Length > 0)
                    {
                        foreach (var roleID in mstUserVM.RoleIds)
                        {
                            userRoles.Add(new DtUserRoles { RoleID = roleID, UserID = mstUserVM.UserID });
                        }
                        // teacher.TeacherSubjects = teacherSubjects;
                        mstUser.DtUserRoles = userRoles;
                    }
                    if (mstUserVM.FacilityIDs.Length > 0)
                    {
                        foreach (var facilityID in mstUserVM.FacilityIDs)
                        {
                            userFacilities.Add(new DtUserFacilities { FacilityID = facilityID, UserID = mstUserVM.UserID });
                        }
                        // teacher.TeacherSubjects = teacherSubjects;
                        mstUser.DtUserFacilities = userFacilities;
                    }
                    //db.Teacher.Add(teacher);
                    //db.SaveChanges();

                    //var mstUserEntity = _mapper.Map<MstUser>(mstUser);

                    _repository.MstUser.CreateUser(mstUser);
                    await _repository.SaveAsync();

                    //var createdUser = _mapper.Map<MstUser>(mstUserEntity);

                    //var mstRole = _repository.MstRole.GetRoleByNameAsync("user");

                    //_repository.DtUserRoles.CreateUserRoles(new EClaimsEntities.Models.DtUserRoles { RoleID = 1, UserID = createdUser.UserID });
                    //await _repository.SaveAsync();

                    _toastNotification.AddSuccessToastMessage("User added successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }

                //mstUser.Name = claims.GetClaim(ClaimTypes.GivenName);
                //mstUser.Surname = claims.GetClaim(ClaimTypes.Surname);
                //var name = claims.GetClaim("name");
                // very rudimentary handling of splitting a users fullname into first and last name. Not very robust.
                //if (string.IsNullOrEmpty(mstUser.Name))
                //{
                //    mstUser.Name = name?.Split(' ').First();
                //}
                //if (string.IsNullOrEmpty(mstUser.Surname))
                //{
                //    var nameSplit = name?.Split(' ');
                //    if (nameSplit.Length > 1)
                //    {
                //        mstUser.Surname = name?.Split(' ').Last();
                //    }
                //}

                // mstUser.EmailAddress = claims.GetClaim(ClaimTypes.Email);
                //  mstUser.EmployeeNo = "MS";

            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateUser action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Add User. Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ViewBankDetails([FromBody] MstUser mstUser)
        {
            int id = Convert.ToInt32(mstUser.UserID);
            var bankDetails =  await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(id);

            BankDetailsViewModel bankDetailsViewModel = new BankDetailsViewModel();

            if (bankDetails != null)
            {
                bankDetailsViewModel.AccountNumber = Aes256CbcEncrypter.Decrypt(bankDetails.AccountNumber);
                bankDetailsViewModel.NameAsInBank = Aes256CbcEncrypter.Decrypt(bankDetails.NameAsInBank);
                bankDetailsViewModel.NameAsInBank = Aes256CbcEncrypter.Decrypt(bankDetails.NameAsInBank);
                bankDetailsViewModel.BankName = Aes256CbcEncrypter.Decrypt(bankDetails.BankName);
                bankDetailsViewModel.BankCode = Aes256CbcEncrypter.Decrypt(bankDetails.BankCode);
                bankDetailsViewModel.Branch = Aes256CbcEncrypter.Decrypt(bankDetails.Branch);
                bankDetailsViewModel.BranchCode = Aes256CbcEncrypter.Decrypt(bankDetails.BranchCode);
                bankDetailsViewModel.PayNow = Aes256CbcEncrypter.Decrypt(bankDetails.PayNow);
                bankDetailsViewModel.BankStatementFileName = Aes256CbcEncrypter.Decrypt(bankDetails.BankStatementFileName);
                bankDetailsViewModel.BankStatementUrl = bankDetails.BankStatementUrl;
            }

            return PartialView("_userBankModal", bankDetailsViewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            UserVM model = new UserVM();
            List<int> roleIds = new List<int>();
            List<int> facilityIds = new List<int>();
            if (id.HasValue)
            {
                //Get user 
                var mstUser = await _repository.MstUser.GetUserByIdAsync(id);
                //Get user roles and add each roleId into roleIds list
                mstUser.DtUserRoles.ToList().ForEach(result => roleIds.Add(result.RoleID));
                mstUser.DtUserFacilities.ToList().ForEach(result => facilityIds.Add(result.FacilityID));
                //bind model 
                model.drpRoles = _repository.MstRole.GetAllRolesAsync().GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.RoleName, Value = x.RoleID.ToString() }).ToList();
                model.drpFacilities = _repository.MstFacility.GetAllFacilityAsync("active").GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.FacilityName, Value = x.FacilityID.ToString() }).ToList();
                model.UserID = mstUser.UserID;
                model.Name = mstUser.Name;
                model.EmailAddress = mstUser.EmailAddress;
                model.EmployeeNo = mstUser.EmployeeNo;
                model.Phone = mstUser.Phone;
                model.IsActive = mstUser.IsActive;
                model.IsHOD = mstUser.IsHOD;
                model.RoleIds = roleIds.ToArray();
                model.FacilityIDs = facilityIds.ToArray();
                model.ExpenseLimit = mstUser.ExpenseLimit;
                model.MileageLimit = mstUser.MileageLimit;
                model.TelephoneLimit = mstUser.TelephoneLimit;
            }
            else
            {
                model = new UserVM();
                model.drpRoles = _repository.MstRole.GetAllRolesAsync().GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.RoleName, Value = x.RoleID.ToString() }).ToList();
                model.drpFacilities = _repository.MstFacility.GetAllFacilityAsync("active").GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.FacilityName, Value = x.FacilityID.ToString() }).ToList();
            }

            return View(model);
            //var mstUser = await _repository.MstUser.GetUserByIdAsync(id);
            //if (mstUser == null)
            //{
            //    return NotFound();
            //}
            //return View(mstUser);
        }

        // POST: Facility/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserVM mstUserVM)
        {
            try
            {
                MstUser mstUser = new MstUser();
                List<DtUserRoles> dtuserRoles = new List<DtUserRoles>();
                List<DtUserFacilities> dtUserFacilities = new List<DtUserFacilities>();
                if (mstUserVM == null)
                {
                    _logger.LogError("User object sent from client is null.");
                    return BadRequest("User object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid user object while add. Error = " + modelErrors);

                    return View(mstUserVM);
                }

                if (mstUserVM.UserID > 0)
                {
                    //first find user roles list and then remove all from db 
                    mstUser = await _repository.MstUser.GetUserByIdAsync(mstUserVM.UserID);

                    if (mstUser == null)
                    {
                        string errorMessage = $"User with id: {id}, hasn't been found in db.";
                        _logger.LogError(errorMessage);
                        _toastNotification.AddErrorToastMessage(errorMessage);
                        return NotFound();
                    }

                    var mstUserEntityMod = _mapper.Map<MstUser>(mstUserVM);

                    if (mstUser.EmailAddress == mstUserVM.EmailAddress && mstUser.EmployeeNo == mstUserVM.EmployeeNo)
                    {
                        mstUser.DtUserRoles.ToList().ForEach(result => dtuserRoles.Add(result));
                        mstUser.DtUserFacilities.ToList().ForEach(result => dtUserFacilities.Add(result));
                        _context.dtUserRoles.RemoveRange(dtuserRoles);
                        _context.dtUserFacilities.RemoveRange(dtUserFacilities);
                        _context.SaveChanges();

                        //Now update user details
                        mstUser.Name = mstUserVM.Name;
                        mstUser.EmployeeNo = mstUserVM.EmployeeNo;
                        mstUser.Phone = mstUserVM.Phone;
                        mstUser.EmailAddress = mstUserVM.EmailAddress;
                        mstUser.IsActive = mstUserVM.IsActive;
                        mstUser.IsHOD = mstUserVM.IsHOD;
                        mstUser.LastModificationTime = DateTime.Now;
                        mstUser.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        //mstUser.FacilityID = mstUserVM.FacilityID;
                        mstUser.ExpenseLimit = mstUserVM.ExpenseLimit;
                        mstUser.MileageLimit = mstUserVM.MileageLimit;
                        mstUser.TelephoneLimit = mstUserVM.TelephoneLimit;
                        if (mstUserVM.RoleIds.Length > 0)
                        {
                            dtuserRoles = new List<DtUserRoles>();

                            foreach (var roleID in mstUserVM.RoleIds)
                            {
                                dtuserRoles.Add(new DtUserRoles { RoleID = roleID, UserID = mstUserVM.UserID });
                            }
                            mstUser.DtUserRoles = dtuserRoles;
                        }
                        if (mstUserVM.FacilityIDs.Length > 0)
                        {
                            dtUserFacilities = new List<DtUserFacilities>();

                            foreach (var facilityID in mstUserVM.FacilityIDs)
                            {
                                dtUserFacilities.Add(new DtUserFacilities { FacilityID = facilityID, UserID = mstUserVM.UserID });
                            }
                            mstUser.DtUserFacilities = dtUserFacilities;
                        }
                        _repository.MstUser.UpdateUser(mstUser);
                        await _repository.SaveAsync();
                        _toastNotification.AddSuccessToastMessage("User updated successfully", new NotyOptions() { Timeout = 5000 });
                        return RedirectToAction("Index");
                    }
                    else if (_repository.MstUser.ValidateUser(mstUserEntityMod, "edit"))
                    {
                        //TempData["Error"] = "User already exists.";
                        //UserVM model = new UserVM();
                        mstUserVM.drpRoles = _repository.MstRole.GetAllRolesAsync().GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.RoleName, Value = x.RoleID.ToString() }).ToList();
                        mstUserVM.drpFacilities = _repository.MstFacility.GetAllFacilityAsync("active").GetAwaiter().GetResult().Select(x => new SelectListItem { Text = x.FacilityName, Value = x.FacilityID.ToString() }).ToList();
                        // ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync(), "DepartmentID", "Department", mstFacility.DepartmentID);
                        _toastNotification.AddErrorToastMessage("User already exists", new NotyOptions() { Timeout = 5000 });
                        return View(mstUserVM);
                    }
                    else
                    {
                        //mstUserEntityFromDB.LastModificationTime = DateTime.Now;
                        //mstUserEntityFromDB.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                        //mstUserEntityFromDB.Name = mstUser.Name;
                        //mstUserEntityFromDB.EmployeeNo = mstUser.EmployeeNo;
                        //mstUserEntityFromDB.Phone = mstUser.Phone;
                        //mstUserEntityFromDB.EmailAddress = mstUser.EmailAddress;

                        //_repository.MstUser.UpdateUser(mstUserEntityFromDB);
                        //await _repository.SaveAsync();


                        mstUser.DtUserRoles.ToList().ForEach(result => dtuserRoles.Add(result));
                        mstUser.DtUserFacilities.ToList().ForEach(result => dtUserFacilities.Add(result));
                        _context.dtUserRoles.RemoveRange(dtuserRoles);
                        _context.dtUserFacilities.RemoveRange(dtUserFacilities);
                        _context.SaveChanges();

                        //Now update user details
                        mstUser.Name = mstUserVM.Name;
                        mstUser.EmployeeNo = mstUserVM.EmployeeNo;
                        mstUser.Phone = mstUserVM.Phone;
                        mstUser.EmailAddress = mstUserVM.EmailAddress;
                        mstUser.NameIdentifier = mstUserVM.EmailAddress;
                        mstUser.UserName = mstUserVM.EmailAddress;
                        mstUser.IsActive = mstUserVM.IsActive;
                        mstUser.IsHOD = mstUserVM.IsHOD;
                        mstUser.LastModificationTime = DateTime.Now;
                        mstUser.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        //mstUser.FacilityID = mstUserVM.FacilityID;
                        mstUser.ExpenseLimit = mstUserVM.ExpenseLimit;
                        mstUser.MileageLimit = mstUserVM.MileageLimit;
                        mstUser.TelephoneLimit = mstUserVM.TelephoneLimit;
                        if (mstUserVM.RoleIds.Length > 0)
                        {
                            dtuserRoles = new List<DtUserRoles>();

                            foreach (var roleID in mstUserVM.RoleIds)
                            {
                                dtuserRoles.Add(new DtUserRoles { RoleID = roleID, UserID = mstUserVM.UserID });
                            }
                            mstUser.DtUserRoles = dtuserRoles;
                        }
                        if (mstUserVM.FacilityIDs.Length > 0)
                        {
                            dtUserFacilities = new List<DtUserFacilities>();

                            foreach (var facilityID in mstUserVM.FacilityIDs)
                            {
                                dtUserFacilities.Add(new DtUserFacilities { FacilityID = facilityID, UserID = mstUserVM.UserID });
                            }
                            mstUser.DtUserFacilities = dtUserFacilities;
                        }
                        _repository.MstUser.UpdateUser(mstUser);
                        await _repository.SaveAsync();
                        _toastNotification.AddSuccessToastMessage("User updated successfully", new NotyOptions() { Timeout = 5000 });
                        return RedirectToAction("Index");
                    }
                }
                return RedirectToAction("index");

                /*
                var mstUserEntityFromDB = await _repository.MstUser.GetUserByIdAsync(id);
                if (mstUserEntityFromDB == null)
                {
                    _logger.LogError($"User with id: {id}, hasn't been found in db.");
                    return NotFound();
                }

                //var mstUserEntityMod = _mapper.Map<MstDepartment>(mstUser);

                if (mstUserEntityFromDB.EmailAddress == mstUser.EmailAddress && mstUserEntityFromDB.EmployeeNo == mstUser.EmployeeNo)
                {
                    mstUserEntityFromDB.LastModificationTime = DateTime.Now;
                    mstUserEntityFromDB.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    mstUserEntityFromDB.Name = mstUser.Name;
                    mstUserEntityFromDB.EmployeeNo = mstUser.EmployeeNo;
                    mstUserEntityFromDB.Phone = mstUser.Phone;
                    mstUserEntityFromDB.EmailAddress = mstUser.EmailAddress;

                    _repository.MstUser.UpdateUser(mstUserEntityFromDB);
                    await _repository.SaveAsync();

                    return RedirectToAction("Index");
                }
                else if (_repository.MstUser.ValidateUser(mstUser))
                {
                    TempData["Error"] = "User already exists.";
                   // ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync(), "DepartmentID", "Department", mstFacility.DepartmentID);
                    return View("Edit");
                }
                else
                {
                    mstUserEntityFromDB.LastModificationTime = DateTime.Now;
                    mstUserEntityFromDB.LastModifierUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    mstUserEntityFromDB.Name = mstUser.Name;
                    mstUserEntityFromDB.EmployeeNo = mstUser.EmployeeNo;
                    mstUserEntityFromDB.Phone = mstUser.Phone;
                    mstUserEntityFromDB.EmailAddress = mstUser.EmailAddress;

                    _repository.MstUser.UpdateUser(mstUserEntityFromDB);
                    await _repository.SaveAsync();

                    return RedirectToAction("Index");
                }
                */
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateUser action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Edit User. Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        public FileResult DownloadFile(string fileName, string bankStatementFileName)
        {
            var MainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
            //Build the File Path.
            string path = Path.Combine(MainPath, fileName);

            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(path);

            byte[] decryptFile = Aes256CbcEncrypter.DecryptFile(bytes);

            //Send the File to Download.
            return File(decryptFile, "application/octet-stream", bankStatementFileName);
        }
    }
}
