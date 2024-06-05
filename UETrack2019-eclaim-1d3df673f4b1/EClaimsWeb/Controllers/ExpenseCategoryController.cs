using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExpenseCategoryController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;

        public ExpenseCategoryController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Facility
        public async Task<IActionResult> Index()
        {
            try
            {
                var mstExpenseCategoriesWithTypes = await _repository.MstExpenseCategory.GetAllExpenseCategoriesWithTypesAsync();
                _logger.LogInfo($"Returned all Expense Categories with types from database.");

                var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(mstExpenseCategoriesWithTypesResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllExpenseCategoriesWithTypesAsync action: {ex.Message}");
                return View();
            }
        }


        // GET: Facility/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType");
            ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure");
            ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MstExpenseCategory mstExpenseCategory)
        {
            try
            {
                if (mstExpenseCategory == null)
                {
                    _logger.LogError("Expense Category object sent from client is null.");
                    return BadRequest("Expense Category object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);

                    ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType", mstExpenseCategory.CostTypeID);
                    ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure", mstExpenseCategory.CostStructureID);
                    ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType", mstExpenseCategory.ClaimTypeID);
                    return View(mstExpenseCategory);

                    //return BadRequest("Invalid model object");
                }

                if (_repository.MstExpenseCategory.ValidateExpenseCategory(mstExpenseCategory, "create"))
                {
                    //TempData["Error"] = "Expense Category already exists.";
                    ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType", mstExpenseCategory.CostTypeID);
                    ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure", mstExpenseCategory.CostStructureID);
                    ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType", mstExpenseCategory.ClaimTypeID);

                    _toastNotification.AddErrorToastMessage("Expense Category already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else
                {
                    mstExpenseCategory.CreatedDate = DateTime.Now;
                    mstExpenseCategory.ModifiedDate = DateTime.Now;
                    mstExpenseCategory.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstExpenseCategory.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstExpenseCategory.ApprovalDate = DateTime.Now;
                    mstExpenseCategory.ApprovalStatus = 3;
                    mstExpenseCategory.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    //var mstExpenseCategoryEntity = _mapper.Map<MstExpenseCategory>(mstExpenseCategory);

                    _repository.MstExpenseCategory.CreateExpenseCategory(mstExpenseCategory);
                    await _repository.SaveAsync();

                    var createdExpenseCategory = _mapper.Map<MstExpenseCategory>(mstExpenseCategory);
                    _toastNotification.AddSuccessToastMessage("Expense Category added successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateExpenseCategory action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Add Expense Category. Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }


        // GET: Facility/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(id);
            if (mstExpenseCategory == null)
            {
                return NotFound();
            }
            ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType", mstExpenseCategory.CostTypeID);
            ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure", mstExpenseCategory.CostStructureID);
            ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType", mstExpenseCategory.ClaimTypeID);
            return View(mstExpenseCategory);
        }

        // POST: Facility/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MstExpenseCategory mstExpenseCategory)
        {
            try
            {
                if (mstExpenseCategory == null)
                {
                    _logger.LogError("Expense Category object sent from client is null.");
                    return BadRequest("Expense Category object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);

                    //return BadRequest("Invalid model object");
                    ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType", mstExpenseCategory.CostTypeID);
                    ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure", mstExpenseCategory.CostStructureID);
                    ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType", mstExpenseCategory.ClaimTypeID);
                    return View(mstExpenseCategory);
                }

                var mstExpenseCategoryEntityFromDB = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(mstExpenseCategory.ExpenseCategoryID);
                if (mstExpenseCategoryEntityFromDB == null)
                {
                    string errorMessage = $"Expense Category with id: {id}, hasn't been found in db.";
                    _logger.LogError(errorMessage);
                    _toastNotification.AddErrorToastMessage(errorMessage);
                    return NotFound();
                }

                //var mstFacilityEntityMod = _mapper.Map<MstDepartment>(mstFacility);

                if (mstExpenseCategoryEntityFromDB.CategoryCode == mstExpenseCategory.CategoryCode && mstExpenseCategoryEntityFromDB.Description == mstExpenseCategory.Description && mstExpenseCategoryEntityFromDB.CostTypeID == mstExpenseCategory.CostTypeID && mstExpenseCategoryEntityFromDB.CostStructureID == mstExpenseCategory.CostStructureID && mstExpenseCategoryEntityFromDB.ClaimTypeID == mstExpenseCategory.ClaimTypeID)
                {
                    mstExpenseCategoryEntityFromDB.ModifiedDate = DateTime.Now;
                    mstExpenseCategoryEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstExpenseCategoryEntityFromDB.CategoryCode = mstExpenseCategory.CategoryCode;
                    mstExpenseCategoryEntityFromDB.Description = mstExpenseCategory.Description;
                    mstExpenseCategoryEntityFromDB.CostTypeID = mstExpenseCategory.CostTypeID;
                    mstExpenseCategoryEntityFromDB.CostStructureID = mstExpenseCategory.CostStructureID;
                    mstExpenseCategoryEntityFromDB.ClaimTypeID = mstExpenseCategory.ClaimTypeID;
                    mstExpenseCategoryEntityFromDB.Default = mstExpenseCategory.Default;
                    mstExpenseCategoryEntityFromDB.IsGSTRequired = mstExpenseCategory.IsGSTRequired;
                    mstExpenseCategoryEntityFromDB.IsActive = mstExpenseCategory.IsActive;
                    mstExpenseCategoryEntityFromDB.ExpenseCode = mstExpenseCategory.ExpenseCode;

                    //_mapper.Map<MstExpenseCategory>(mstExpenseCategory);

                    _repository.MstExpenseCategory.UpdateExpenseCategory(mstExpenseCategoryEntityFromDB);
                    await _repository.SaveAsync();

                    _toastNotification.AddSuccessToastMessage("Expense Category updated successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
                else if (_repository.MstExpenseCategory.ValidateExpenseCategory(mstExpenseCategory, "edit"))
                {
                    //TempData["Error"] = "Expense Category already exists.";
                    ViewData["CostTypeID"] = new SelectList(await _repository.MstCostType.GetAllCostTypeAsync(), "CostTypeID", "CostType", mstExpenseCategory.CostTypeID);
                    ViewData["CostStructureID"] = new SelectList(await _repository.MstCostStructure.GetAllCostStructureAsync(), "CostStructureID", "CostStructure", mstExpenseCategory.CostStructureID);
                    ViewData["ClaimTypeID"] = new SelectList(await _repository.MstClaimType.GetAllClaimTypeAsync(), "ClaimTypeID", "ClaimType", mstExpenseCategory.ClaimTypeID);
                    _toastNotification.AddErrorToastMessage("Expense Category already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else
                {
                    mstExpenseCategoryEntityFromDB.ModifiedDate = DateTime.Now;
                    mstExpenseCategoryEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstExpenseCategoryEntityFromDB.CategoryCode = mstExpenseCategory.CategoryCode;
                    mstExpenseCategoryEntityFromDB.Description = mstExpenseCategory.Description;
                    mstExpenseCategoryEntityFromDB.CostTypeID = mstExpenseCategory.CostTypeID;
                    mstExpenseCategoryEntityFromDB.CostStructureID = mstExpenseCategory.CostStructureID;
                    mstExpenseCategoryEntityFromDB.ClaimTypeID = mstExpenseCategory.ClaimTypeID;
                    mstExpenseCategoryEntityFromDB.Default = mstExpenseCategory.Default;
                    mstExpenseCategoryEntityFromDB.IsGSTRequired = mstExpenseCategory.IsGSTRequired;
                    mstExpenseCategoryEntityFromDB.IsActive = mstExpenseCategory.IsActive;
                    mstExpenseCategoryEntityFromDB.ExpenseCode = mstExpenseCategory.ExpenseCode;

                    //_mapper.Map<MstExpenseCategory>(mstExpenseCategory);

                    _repository.MstExpenseCategory.UpdateExpenseCategory(mstExpenseCategoryEntityFromDB);
                    await _repository.SaveAsync();
                    _toastNotification.AddSuccessToastMessage("Expense Category updated successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateExpenseCategory action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Edit Expense Category. Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
