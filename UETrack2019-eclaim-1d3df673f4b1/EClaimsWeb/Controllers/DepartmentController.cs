using AutoMapper;
using EClaimsRepository.Contracts;
using EClaimsEntities.DataTransferObjects;
using EClaimsEntities.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using NToastNotify;
using EClaimsWeb.Helpers;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;

        public DepartmentController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, IToastNotification toastNotification)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _toastNotification = toastNotification;
        }
         [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var mstDepartments = await _repository.MstDepartment.GetAllDepartmentAsync();
                _logger.LogInfo($"Returned all departments from database.");

                var mstDepartmentsResult = _mapper.Map<IEnumerable<DepartmentViewModel>>(mstDepartments);
                return View(mstDepartmentsResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllDepartmentAsync action: {ex.Message}");
                return View();
            }
        }
         [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentViewModel mstDepartment)
        {
            try
            {
                if (mstDepartment == null)
                {
                    _logger.LogError("Department object sent from client is null.");
                    return BadRequest("Department object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);
                    return View(mstDepartment);
                }

                var mstDepartmentEntity = _mapper.Map<MstDepartment>(mstDepartment);

                if (_repository.MstDepartment.ValidateDepartment(mstDepartmentEntity,"create"))
                {
                    _toastNotification.AddErrorToastMessage("Department already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else
                {
                    mstDepartment.CreatedDate = DateTime.Now;
                    mstDepartment.ModifiedDate = DateTime.Now;
                    mstDepartment.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstDepartment.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstDepartment.ApprovalDate = DateTime.Now;
                    mstDepartment.ApprovalStatus = 3;
                    mstDepartment.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    mstDepartmentEntity = _mapper.Map<MstDepartment>(mstDepartment);

                    var entity = _repository.MstDepartment.CreateAndReturnDepartment(mstDepartmentEntity);

                    //_repository.MstDepartment.CreateDepartment(mstDepartmentEntity);
                    await _repository.SaveAsync();

                    await _repository.MstDepartment.InsertApprovalMatrixForDepartment(entity.DepartmentID, Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    await _repository.SaveAsync();

                    var createdDepartment = _mapper.Map<DepartmentViewModel>(mstDepartmentEntity);
                }
                _toastNotification.AddSuccessToastMessage("Department added successfully", new NotyOptions() { Timeout = 5000 });
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Add Department. Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }
         [Authorize(Roles = "Admin")]
        // GET: Facility/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mstDepartment = await _repository.MstDepartment.GetDepartmentByIdAsync(id);
            if (mstDepartment == null)
            {
                return NotFound();
            }
            return View(mstDepartment);
        }

         [Authorize(Roles = "Admin")]
        // POST: Facility/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, DepartmentViewModel mstDepartment)
        {
            try
            {
                if (mstDepartment == null)
                {
                    _logger.LogError("Facility object sent from client is null.");
                    return BadRequest("Facility object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);
                    return View(mstDepartment);
                }

                var mstDepartmentEntityFromDB = await _repository.MstDepartment.GetDepartmentByIdAsync(mstDepartment.DepartmentID);
                if (mstDepartmentEntityFromDB == null)
                {
                    string errorMessage = $"Department with id: {id}, hasn't been found in db.";
                    _logger.LogError(errorMessage);
                    _toastNotification.AddErrorToastMessage(errorMessage);
                    return NotFound();
                }

                var mstDepartmentEntityMod = _mapper.Map<MstDepartment>(mstDepartment);

                if(mstDepartmentEntityFromDB.Code == mstDepartmentEntityMod.Code && mstDepartmentEntityFromDB.Department == mstDepartmentEntityMod.Department)
                {
                    mstDepartmentEntityFromDB.ModifiedDate = DateTime.Now;
                    mstDepartmentEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstDepartmentEntityFromDB.Code = mstDepartmentEntityMod.Code;
                    mstDepartmentEntityFromDB.Department = mstDepartmentEntityMod.Department;
                    mstDepartmentEntityFromDB.IsActive = mstDepartmentEntityMod.IsActive;

                    _repository.MstDepartment.UpdateDepartment(mstDepartmentEntityFromDB);
                    await _repository.SaveAsync();

                    _toastNotification.AddSuccessToastMessage("Department updated successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");

                }
                else if (_repository.MstDepartment.ValidateDepartment(mstDepartmentEntityMod,"edit"))
                {
                    _toastNotification.AddErrorToastMessage("Department already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else 
                {
                    mstDepartmentEntityFromDB.ModifiedDate = DateTime.Now;
                    mstDepartmentEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstDepartmentEntityFromDB.Code = mstDepartmentEntityMod.Code;
                    mstDepartmentEntityFromDB.Department = mstDepartmentEntityMod.Department;
                    mstDepartmentEntityFromDB.IsActive = mstDepartmentEntityMod.IsActive;

                    _repository.MstDepartment.UpdateDepartment(mstDepartmentEntityFromDB);
                    await _repository.SaveAsync();
                    _toastNotification.AddSuccessToastMessage("Department updated successfully");
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateDepartment action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Edit Department. Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
