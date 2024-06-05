using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace EClaimsWeb.Controllers
{
    public class ApprovalSettingsController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;

        private readonly RepositoryContext _context;

        public ApprovalSettingsController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        [HttpPost]
        public async Task<JsonResult> GetApprovers(string custId)
        {
            var approvers = new SelectList(await _repository.MstUser.GetAllUsersAsync(), "UserID", "Name");
           

            return Json(approvers);
        }

        [HttpPost]
        public JsonResult AjaxMethod()
        {
            List<SelectListItem> customers = new List<SelectListItem>();
            var users = _repository.MstUser.GetAllUsersAsync();
            for (int i = 0; i < users.Result.Count(); i++)
            {
                customers.Add(new SelectListItem
                {
                    Value = users.Result.ToList()[i].UserID.ToString(),
                    Text = users.Result.ToList()[i].Name
                    //Value = entities.Customers.ToList()[i].CustomerID,
                    //Text = entities.Customers.ToList()[i].ContactName
                });
            }
            return Json(customers);
        }

        [HttpPost]
        public JsonResult ReturnJSONDataToAJax()
        {
            //List<SelectListItem> customers = new List<SelectListItem>();
            var users = _repository.MstUser.GetAllUsersAsync().Result.ToList();
            //for (int i = 0; i < users.Result.Count(); i++)
            //{
            //    customers.Add(new SelectListItem
            //    {
            //        Value = users.Result.ToList()[i].UserID.ToString(),
            //        Text = users.Result.ToList()[i].Name
            //        //Value = entities.Customers.ToList()[i].CustomerID,
            //        //Text = entities.Customers.ToList()[i].ContactName
            //    });
            //}
            return Json(users);
        }

        // GET: Facility
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["ddlApprover"] = new SelectList(await _repository.MstUser.GetAllUsersAsync(), "UserID", "Name");
                ViewData["ddlVerifier"] = new SelectList(await _repository.MstUser.GetAllUsersAsync(), "UserID", "Name");

                List<clsModule> oclsModule = new List<clsModule>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsModule.Add(new clsModule() { ModuleName = "Claims", ModuleId = "Claims" });
                oclsModule.Add(new clsModule() { ModuleName = "HR", ModuleId = "HR" });
                oclsModule.Add(new clsModule() { ModuleName = "Finance", ModuleId = "Finance" });
                //var ddlModule = (from t in oclsModule
                //                 select new SelectListItem
                //                 {
                //                     Text = t.ModuleName.ToString(),
                //                     Value = t.ModuleId.ToString(),
                //                 }).OrderBy(p => p.Text).ToList();
              
                //ViewData["ModuleId"] = new SelectList(ddlModule, "ModuleId", "ModuleName");

                ViewBag.ModuleList = oclsModule;
                //  ViewData["ScreenID"] = new SelectList(await _repository.MstScreens.GetAllScreensAsync(), "ScreenID", "ScreenName");

                //List<SelectListItem> ddlScreen = new List<SelectListItem>();

                //ViewData["ScreenId"] = ddlScreen;

                //var mstApprovalMatrixByScreens =  _repository.MstApprovalMatrix.GetApprovalMatrixByScreens(1);
                //_logger.LogInfo($"Returned all Approval Matrix By Screens  from database.");

                //var mstApprovalMatrixByScreensResult = _mapper.Map<IEnumerable<MstApprovalMatrix>>(mstApprovalMatrixByScreens);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetApprovalMatrixByScreens action: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public ActionResult AjaxHandler(jQueryDataTableParamModel param)
        {
           // int InstantId = Convert.ToInt32(Session["InstanceID"]);
            List<clsApprovalSettings> AllApprovalSettings = new List<clsApprovalSettings>();
            List<MstApprovalMatrix> list = new List<MstApprovalMatrix>();
            string module = "";
            string screen = "";
           
            //int project = 0;
            if (param.ddlModule != null)
            {
                module = param.ddlModule;
               
            }

            if(param.ddlScreen !=null)
            {
                screen = param.ddlScreen;
                list = _repository.MstApprovalMatrix.GetApprovalMatrixByScreens(Convert.ToInt32(screen)).ToList();
            }
            
           // var list = objERPEntities.GetApprovalSettings(module, screen, department, hq, 0, facility).ToList();

            foreach (var item in list)
            {
                clsApprovalSettings objclsApprovalSettings = new clsApprovalSettings();
                objclsApprovalSettings.AMID = item.AMID != null ? item.AMID : 0;
                objclsApprovalSettings.ScreenName = item.MstScreens.ScreenName != null ? item.MstScreens.ScreenName.ToString() : "";
               // objclsApprovalSettings.Department = item.Department != null ? item.Department.ToString() : "";
               // objclsApprovalSettings.FacilityName = item.FacilityName != null ? item.FacilityName.ToString() : "";
                objclsApprovalSettings.ScreenID = (int)item.ScreenID != null ? (int)item.ScreenID : 0;
                objclsApprovalSettings.ApprovalRequired = (bool)item.ApprovalRequired;
                objclsApprovalSettings.VerificationLevels = (int)item.VerificationLevels != null ? (int)item.VerificationLevels : 0;
                objclsApprovalSettings.ApprovalLevels = (int)item.ApprovalLevels != null ? (int)item.ApprovalLevels : 0;

              

                AllApprovalSettings.Add(objclsApprovalSettings);
            }

            IEnumerable<clsApprovalSettings> filteredCompanies = AllApprovalSettings;//Getresult(param, AllApprovalSettings);

            var displayedCompanies = filteredCompanies
            .Skip(param.iDisplayStart)
                   .Take(param.iDisplayLength);

           // string img = VirtualPathUtility.ToAbsolute("~/img/edit.png");

            var result = from c in displayedCompanies
                         select new object[] {
                             c.AMID,
                             //c.Department == "" ? c.ScreenName + "-" + c.FacilityName : c.ScreenName + "-" + c.Department + "-" + c.FacilityName, 
                              c.ScreenName,
                            "<input checked='checked' class='clsCheckBox' id='status' name='status' type='checkbox' value='true'><input name = 'status' type='hidden' value='false'>",
                            c.ApprovalRequired == true ? "<input class='clsTextBox' id='txtVerification' maxlength='2' name='txtVerification' ondrop='return false;' onkeypress='return isNumber(event)' onpaste='return false' style='width: 110px;' type='text' value="+ c.VerificationLevels +">" : "<input class='clsTextBox' id='txtVerification' maxlength='2' name='txtVerification' ondrop='return false;' onkeypress='return isNumber(event)' onpaste='return false' style='width: 110px;' type='text' value="+ c.VerificationLevels +">" ,
                            c.ApprovalRequired == true ? "<input class='clsTextBox' id='txtApproval' maxlength='2' name='txtApproval' ondrop='return false;' onkeypress='return isNumber(event)' onpaste='return false' style='width: 110px;' type='text' value="+ c.ApprovalLevels +">" : "<input class='clsTextBox' id='txtApproval' maxlength='2' name='txtApproval' ondrop='return false;' onkeypress='return isNumber(event)' onpaste='return false' style='width: 110px;' type='text' value="+ c.ApprovalLevels +">" ,
                                         "<a href='javascript:void(0);' id='btnedit' onclick='EditUser("+ c.AMID +")'>Add/Edit</a>"

                                      };


            return Json(new
            {
                sEcho = param.sEcho,
                iTotalRecords = list.Count(),
                iTotalDisplayRecords = filteredCompanies.Count(),
                aaData = result
            });

        }

     

        public JsonResult GetScreensByModule(string moduleID)
        {
            //Need to convert this to repository method
            var data = _repository.MstScreens.FindByCondition(x => x.ModuleName == moduleID).ToList();
            return Json(data);
        }

        public JsonResult getdata(string id)
        {

            var ApprovalMatrices = _repository.DtApprovalMatrix.GetDtApprovalMatrixByAMID(Convert.ToInt32(id));
            //var dtApprovalMatrices = (from t in ApprovalMatrices
            //                          select new clsApprover

            //                          {
            //                              AMID = (int)t.AMID,
            //                              Approver = (int)t.Approver,
            //                              Verifier = (int)t.Verifier,
            //                              AmountFrom = t.AmountFrom,
            //                              AmountTo = t.AmountTo
            //                          }).ToList();

            return Json(ApprovalMatrices);
        }

        [HttpPost]
        public async Task<JsonResult> SaveApprover1(IEnumerable<DtApprovalMatrix> dtApprovalMatrix)
        {
            try
            {
                if (dtApprovalMatrix == null)
                {
                    _logger.LogError("Details Approval matrix object sent from client is null.");
                    //return BadRequest("Details Approval matrix object is null");
                    return Json(new { res = false });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Details Approval matrix object sent from client.");
                    return Json(new { res = false });
                    //return View(mstExpenseCategory);
                    //return BadRequest("Invalid model object");
                }
                //mstExpenseCategory.CreatedDate = DateTime.Now;
                //mstExpenseCategory.ModifiedDate = DateTime.Now;
                //mstExpenseCategory.CreatedBy = 1;
                //mstExpenseCategory.ModifiedBy = 1;
                //mstExpenseCategory.ApprovalDate = DateTime.Now;
                //mstExpenseCategory.ApprovalStatus = 3;
                //mstExpenseCategory.ApprovalBy = 1;

                //var mstExpenseCategoryEntity = _mapper.Map<MstExpenseCategory>(mstExpenseCategory);
                foreach (var dtAM in dtApprovalMatrix)
                {
                    _repository.DtApprovalMatrix.Delete(dtAM);
                    await _repository.SaveAsync();
                }

                foreach (var dtAM in dtApprovalMatrix)
                {
                    _repository.DtApprovalMatrix.Create(dtAM);
                    await _repository.SaveAsync();
                }
                return Json(new { res = true });
                //_repository.MstExpenseCategory.CreateExpenseCategory(mstExpenseCategoryEntity);
                //await _repository.SaveAsync();

                //var createdExpenseCategory = _mapper.Map<MstExpenseCategory>(mstExpenseCategoryEntity);


                // return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateExpenseCategory action: {ex.Message}");
                //return RedirectToAction("Index");
                return Json(new { res = false });
            }

        }


        [HttpPost]
        public JsonResult SaveItems(string data)
        {

            try
            {
                List<MstApprovalMatrix> mstApprovalMatrix = JsonConvert.DeserializeObject<List<MstApprovalMatrix>>(data);

                foreach (var mstAM in mstApprovalMatrix)
                {
                    if (!mstAM.ApprovalRequired)
                    {
                        mstAM.VerificationLevels = 0;
                        mstAM.ApprovalLevels = 0;
                    }
                    mstAM.ModifiedBy = 1;
                    mstAM.ModifiedDate = DateTime.Now;
                    _repository.MstApprovalMatrix.UpdateApprovalMatrix(mstAM);
                    _repository.SaveAsync();
                }
            }
            catch { }
            return Json(new { res = true });
        }

        [HttpPost]
        public JsonResult SaveApprover(string data)
        {
            try
            {
                //List<DtApprovalMatrix> dtApprovalMatrix;

                List<DtApprovalMatrix> dtApprovalMatrix = JsonConvert.DeserializeObject<List<DtApprovalMatrix>>(data);

                List<DtApprovalMatrix> dtApprovalMatrixCopy = dtApprovalMatrix;

                if (dtApprovalMatrix == null)
                {
                    _logger.LogError("Details Approval matrix object sent from client is null.");
                    //return BadRequest("Details Approval matrix object is null");
                    return Json(new { res = false });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Details Approval matrix object sent from client.");
                    return Json(new { res = false });
                    //return View(mstExpenseCategory);
                    //return BadRequest("Invalid model object");
                }
                //mstExpenseCategory.CreatedDate = DateTime.Now;
                //mstExpenseCategory.ModifiedDate = DateTime.Now;
                //mstExpenseCategory.CreatedBy = 1;
                //mstExpenseCategory.ModifiedBy = 1;
                //mstExpenseCategory.ApprovalDate = DateTime.Now;
                //mstExpenseCategory.ApprovalStatus = 3;
                //mstExpenseCategory.ApprovalBy = 1;

                //var mstExpenseCategoryEntity = _mapper.Map<MstExpenseCategory>(mstExpenseCategory);
                var IsFirstDeleted = false;
                foreach (var dtAM in dtApprovalMatrixCopy)
                {
                    if (!IsFirstDeleted)
                    {
                        var entitydtAM = _repository.DtApprovalMatrix.GetDtApprovalMatrixByAMID(dtAM.AMID);
                        foreach (var eDAM in entitydtAM)
                        {
                            eDAM.ModifiedBy = 1;
                            eDAM.ModifiedDate = DateTime.Now;
                            _repository.DtApprovalMatrix.DeleteDtApprovalMatrix(eDAM);
                            IsFirstDeleted = true;
                        }
                        _repository.SaveAsync();
                    }
                    //var mstExpenseCategoryEntity = _mapper.Map<DtApprovalMatrix>(dtAM);
                    //_repository.DtApprovalMatrix.Delete(dtAM);
                    //_repository.SaveAsync();
                }



                foreach (var dtAM1 in dtApprovalMatrix)
                {
                    dtAM1.ModifiedBy = 1;
                    dtAM1.ModifiedDate = DateTime.Now;
                    //var mstExpenseCategoryEntity = _mapper.Map<DtApprovalMatrix>(clsApprovers);
                    _repository.DtApprovalMatrix.CreateDtApprovalMatrix(dtAM1);

                }
                _repository.SaveAsync();
                return Json(new { res = true });
                //_repository.MstExpenseCategory.CreateExpenseCategory(mstExpenseCategoryEntity);
                //await _repository.SaveAsync();

                //var createdExpenseCategory = _mapper.Map<MstExpenseCategory>(mstExpenseCategoryEntity);


                // return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateExpenseCategory action: {ex.Message}");
                //return RedirectToAction("Index");
                return Json(new { res = false });
            }

        }

    }

    public class clsApprover
    {
        public int AMID { get; set; }
        public int Verifier { get; set; }
        public int Approver { get; set; }
        public string AmountFrom { get; set; }
        public string AmountTo { get; set; }


    }

    public class clsApprovalSettings
    {
        public int AMID { get; set; }
        public string ScreenName { get; set; }
        public string Department { get; set; }
        public string FacilityName { get; set; }
        public int ScreenID { get; set; }
        public bool ApprovalRequired { get; set; }
        public int VerificationLevels { get; set; }
        public int ApprovalLevels { get; set; }

    }

    public class clsModule
    {
        public string ModuleId { get; set; }
        public string ModuleName { get; set; }

    }
    #region -- Class --

    public class jQueryDataTableParamModel
    {
        /// <summary>
        /// Request sequence number sent by DataTable,
        /// same value must be returned in response
        /// </summary>       
        public string sEcho { get; set; }

        /// <summary>
        /// Text used for filtering
        /// </summary>
        public string sSearch { get; set; }

        /// <summary>
        /// Number of records that should be shown in table
        /// </summary>
        public int iDisplayLength { get; set; }

        /// <summary>
        /// First record that should be shown(used for paging)
        /// </summary>
        public int iDisplayStart { get; set; }

        /// <summary>
        /// Number of columns in table
        /// </summary>
        public int iColumns { get; set; }

        /// <summary>
        /// Number of columns that are used in sorting
        /// </summary>
        public int iSortingCols { get; set; }

        /// <summary>
        /// Comma separated list of column names
        /// </summary>
        public string sColumns { get; set; }

        public string firstcriteria { get; set; }

        public string ddlModule { get; set; }

        public string ddlScreen { get; set; }
    }

    #endregion

}
