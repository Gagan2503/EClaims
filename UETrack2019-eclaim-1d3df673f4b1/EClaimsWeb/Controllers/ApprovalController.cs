using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApprovalController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;

        private readonly RepositoryContext _context;

        public ApprovalController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }


        public async Task<IActionResult> Index(int screenId, string moduleName, int? lScreenId,int? departmentID, string searchModule)
        {
            var mstDepartments = await _repository.MstDepartment.GetAllDepartmentAsync("active");
            List<SelectListItem> departments = (from t in mstDepartments
                                            select new SelectListItem
                                            {
                                                Text = t.Department.ToString(),
                                                Value = t.DepartmentID.ToString(),
                                            }).OrderBy(p => p.Text).ToList();

            var departmenttip = new SelectListItem()
            {
                Value = null,
                Text = "--- Select Department ---"
            };

            departments.Insert(0, departmenttip);
            //return new SelectList(departments, "Value", "Text");

            var mstApprovalMatrices = await _repository.MstApprovalMatrix.GetAllApprovalMatrixAsync();
                                      //select m;

            //var mstApprovalMatrices = _context.MstApprovalMatrix.Include(m => m.MstScreens).Select(m=> m.MstScreens.ModuleName);


            //if (!string.IsNullOrEmpty(searchModule))
            //{
            //    movies = mstApprovalMatrices.Where(s => s.mod .Contains(searchModule));
            //}

            if (lScreenId != 0)
            {
                mstApprovalMatrices = mstApprovalMatrices.Where(x => x.ScreenID == lScreenId && x.DepartmentID == departmentID);
            }

            var mstApprovalMatricesVM = new ApprovalMatrixViewModel
            {
                //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                ApprovalMatrices =  mstApprovalMatrices.ToList(),
                Modules = GetModules(),
              LScreens = GetScreens(moduleName),
              Departments = new SelectList(departments, "Value", "Text") 
            };

            return View(mstApprovalMatricesVM);
        }

        public IEnumerable<SelectListItem> GetModules()
        {
            List<clsModule> oclsModule = new List<clsModule>();
            //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
            oclsModule.Add(new clsModule() { ModuleName = "User", ModuleId = "User" });
            oclsModule.Add(new clsModule() { ModuleName = "HR", ModuleId = "HR" });

            List<SelectListItem> modules = (from t in oclsModule
                                            select new SelectListItem
                                            {
                                                Text = t.ModuleName.ToString(),
                                                Value = t.ModuleId.ToString(),
                                            }).OrderBy(p => p.Text).ToList();

            var moduletip = new SelectListItem()
            {
                Value = null,
                Text = "--- Select Module ---"
            };

            modules.Insert(0, moduletip);
            return new SelectList(modules, "Value", "Text");

        }

        public IEnumerable<SelectListItem> GetScreens(string moduleName)
        {
            List<SelectListItem> screens = _repository.MstScreens.FindByCondition(x => x.ModuleName == moduleName)
                .OrderBy(n => n.ScreenName)
                .Select(n =>
                    new SelectListItem
                    {
                        Value = n.ScreenID.ToString(),
                        Text = n.ScreenName
                    }).ToList();

            var screentip = new SelectListItem()
            {
                Value = null,
                Text = "--- Select Sub Module ---"
            };

            screens.Insert(0, screentip);
            return new SelectList(screens, "Value", "Text");
        }

        [HttpPost]
        public async Task<JsonResult> ReturnJSONDataToAJax()
        {
            //List<SelectListItem> customers = new List<SelectListItem>();
            var mstRole = await _repository.MstRole.GetRoleByNameAsync("finance");
            var users = _repository.DtUserRoles.GetAllUsersByRoleIdAsync(mstRole.RoleID);
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
            return Json(users.OrderBy(user => user.Name));
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
        public async Task<JsonResult> SaveItems(string data)
        {

            try
            {
                List<MstApprovalMatrix> mstApprovalMatrix = JsonConvert.DeserializeObject<List<MstApprovalMatrix>>(data);

            
                if (mstApprovalMatrix == null)
                {
                    _logger.LogError("Approval matrix object sent from client is null.");
                    //return BadRequest("Details Approval matrix object is null");
                    return Json(new { res = false });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Approval matrix object sent from client.");
                    return Json(new { res = false });
                    //return View(mstExpenseCategory);
                    //return BadRequest("Invalid model object");
                }

                int iAMID = mstApprovalMatrix.Select(mstAM => mstAM.AMID).FirstOrDefault();
                var entitymstAM = _repository.MstApprovalMatrix.GetApprovalMatrixByIdAsync(iAMID);

                if (entitymstAM is null)
                {
                    _logger.LogError("Approval matrix object sent from DB.");
                    return Json(new { res = false });
                }

                foreach (var mstAM in mstApprovalMatrix)
                {
                    if (!mstAM.ApprovalRequired)
                    {
                        mstAM.VerificationLevels = 0;
                        mstAM.ApprovalLevels = 0;
                    }
                    mstAM.ModifiedBy = 1;
                    mstAM.ScreenID = entitymstAM.Result.ScreenID;
                    mstAM.DepartmentID = entitymstAM.Result.DepartmentID;
                    //mstAM.ModifiedDate = DateTime.Now;
                    _repository.MstApprovalMatrix.UpdateApprovalMatrix(mstAM);
                    await _repository.SaveAsync();
                }
            }
            catch { }
            return Json(new { res = true });
        }

        [HttpPost]
        public async Task<JsonResult> SaveApprover(string data)
        {
            try
            {
                //List<DtApprovalMatrix> dtApprovalMatrix;

                List<DtApprovalMatrix> dtApprovalMatrix = JsonConvert.DeserializeObject<List<DtApprovalMatrix>>(data);
                int AMID = 0;
                // List<DtApprovalMatrix> dtApprovalMatrixCopy = dtApprovalMatrix;

            

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
                int iAMID = dtApprovalMatrix.Select(dtAM => dtAM.AMID).FirstOrDefault();
                var entitydtAM = _repository.DtApprovalMatrix.GetDtApprovalMatrixByAMID(iAMID);

                if(entitydtAM.Count() != 0)
                {
                    foreach(var dtAM in entitydtAM)
                    {
                        _repository.DtApprovalMatrix.Delete(dtAM);
                        //_repository.DtApprovalMatrix.DeleteRange(entitydtAM);
                        await _repository.SaveAsync();
                    }
                    
                    //await _repository.DtApprovalMatrix.DeleteDtApprovalMatrixAsync(iAMID);
                }
                    foreach (var dtAM1 in dtApprovalMatrix)
                    {
                        // dtAM1.ModifiedBy = 1;
                        //dtAM1.ModifiedDate = DateTime.Now;
                        //var mstExpenseCategoryEntity = _mapper.Map<DtApprovalMatrix>(clsApprovers);
                        _repository.DtApprovalMatrix.CreateDtApprovalMatrix(dtAM1);
                    //_repository.DtApprovalMatrix.CreateDtApprovalMatrixAsync(dtAM1);
                         await _repository.SaveAsync();

                    }

                //var IsFirstDeleted = false;
                //foreach (var dtAM in dtApprovalMatrix)
                //{
                //    if (!IsFirstDeleted)
                //    {
                //        var entitydtAM = _repository.DtApprovalMatrix.GetDtApprovalMatrixByAMID(dtAM.AMID);
                //        //AMID = dtAM.AMID;
                //        _repository.DtApprovalMatrix.DeleteDtApprovalMatrix(dtAM);
                //        //_repository.DtApprovalMatrix.DeleteDtApprovalMatrixAsync(AMID);
                //        //var entitydtAM = _repository.DtApprovalMatrix.GetDtApprovalMatrixByAMID(dtAM.AMID);
                //        //foreach (var eDAM in entitydtAM)
                //        //{
                //        //    _repository.DtApprovalMatrix.DeleteDtApprovalMatrixAsync(eDAM.AMID);
                //        //    //eDAM.ModifiedBy = 1;
                //        //    //eDAM.ModifiedDate = DateTime.Now;
                //        //    //_repository.DtApprovalMatrix.DeleteDtApprovalMatrix(eDAM);
                //        //    //IsFirstDeleted = true;
                //        //}
                //        _repository.SaveAsync();
                //    }
                //    //var mstExpenseCategoryEntity = _mapper.Map<DtApprovalMatrix>(dtAM);
                //    //_repository.DtApprovalMatrix.Delete(dtAM);
                //    //_repository.SaveAsync();
                //}


                //foreach (var dtAM1 in dtApprovalMatrix)
                //{
                //   // dtAM1.ModifiedBy = 1;
                //    //dtAM1.ModifiedDate = DateTime.Now;
                //    //var mstExpenseCategoryEntity = _mapper.Map<DtApprovalMatrix>(clsApprovers);
                //    _repository.DtApprovalMatrix.CreateDtApprovalMatrix(dtAM1);
                //    //_repository.DtApprovalMatrix.CreateDtApprovalMatrixAsync(dtAM1);
                //    _repository.SaveAsync();

                //}
                //_repository.SaveAsync();
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
}
