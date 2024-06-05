using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FacilityController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;


        public FacilityController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper,
            RepositoryContext context, IToastNotification toastNotification)
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
                var mstFacilitiesWithDepartments = await _repository.MstFacility.GetAllFacilitiesWithDepartmentsAsync();
                _logger.LogInfo($"Returned all facilities with departments from database.");

                var mstFacilitiesWithDepartmentsResult = _mapper.Map<IEnumerable<MstFacility>>(mstFacilitiesWithDepartments);
                return View(mstFacilitiesWithDepartmentsResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllFacilitiesWithDepartmentsAsync action: {ex.Message}");
                return View();
            }
        }


        // GET: Facility/Create
        public async Task<IActionResult> Create()
        {
            ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department");
            ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MstFacility mstFacility)
        {
            try
            {
                if (mstFacility == null)
                {
                    _logger.LogError("Facility object sent from client is null.");
                    return BadRequest("Facility object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);

                    ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department", mstFacility.DepartmentID);
                    ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name",mstFacility.UserID);
                    return View(mstFacility);
                    //return BadRequest("Invalid model object");
                }

                if (_repository.MstFacility.ValidateFacility(mstFacility, "create"))
                {
                   // TempData["Error"] = "Facility already exists.";
                    ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department", mstFacility.DepartmentID);
                    ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name", mstFacility.UserID);
                    _toastNotification.AddErrorToastMessage("Facility already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else
                {
                    mstFacility.CreatedDate = DateTime.Now;
                    mstFacility.ModifiedDate = DateTime.Now;
                    mstFacility.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstFacility.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstFacility.ApprovalDate = DateTime.Now;
                    mstFacility.ApprovalStatus = 3;
                    mstFacility.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    var mstFacilityEntity = _mapper.Map<MstFacility>(mstFacility);

                    _repository.MstFacility.CreateFacility(mstFacilityEntity);
                    await _repository.SaveAsync();

                    var createdFacility = _mapper.Map<MstFacility>(mstFacilityEntity);

                    _toastNotification.AddSuccessToastMessage("Facility added successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateFacility action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Add Facility. Error: {ex.Message}");
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

            var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(id);
            if (mstFacility == null)
            {
                return NotFound();
            }
            ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department", mstFacility.DepartmentID);
            ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name", mstFacility.UserID);
            return View(mstFacility);
        }

        // POST: Facility/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MstFacility mstFacility)
        {
            try
            {
                if (mstFacility == null)
                {
                    _logger.LogError("Facility object sent from client is null.");
                    return BadRequest("Facility object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid department while add. Error = " + modelErrors);
                    ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department", mstFacility.DepartmentID);
                    ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name", mstFacility.UserID);
                    return View(mstFacility);
                }

                var mstFacilityEntityFromDB = await _repository.MstFacility.GetFacilityByIdAsync(mstFacility.FacilityID);
                if (mstFacilityEntityFromDB == null)
                {
                    string errorMessage = $"Facility with id: {id}, hasn't been found in db.";
                    _logger.LogError(errorMessage);
                    _toastNotification.AddErrorToastMessage(errorMessage);
                    return NotFound();
                }

                //var mstFacilityEntityMod = _mapper.Map<MstDepartment>(mstFacility);

                if (mstFacilityEntityFromDB.Code == mstFacility.Code && mstFacilityEntityFromDB.FacilityName == mstFacility.FacilityName && mstFacilityEntityFromDB.DepartmentID == mstFacility.DepartmentID && mstFacilityEntityFromDB.UserID == mstFacility.UserID)
                {
                    mstFacilityEntityFromDB.ModifiedDate = DateTime.Now;
                    mstFacilityEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstFacilityEntityFromDB.Code = mstFacility.Code;
                    mstFacilityEntityFromDB.DepartmentID = mstFacility.DepartmentID;
                    mstFacilityEntityFromDB.UserID = mstFacility.UserID;
                    mstFacilityEntityFromDB.FacilityName = mstFacility.FacilityName;
                    mstFacilityEntityFromDB.IsActive = mstFacility.IsActive;


                    _repository.MstFacility.UpdateFacility(mstFacilityEntityFromDB);
                    await _repository.SaveAsync();

                    _toastNotification.AddSuccessToastMessage("Facility updated successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
                else if (_repository.MstFacility.ValidateFacility(mstFacility, "edit"))
                {
                   // TempData["Error"] = "Facility already exists.";
                    ViewData["DepartmentID"] = new SelectList(await _repository.MstDepartment.GetAllDepartmentAsync("active"), "DepartmentID", "Department", mstFacility.DepartmentID);
                    ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllHODUsersAsync(), "UserID", "Name", mstFacility.UserID);
                    //return View("Edit");
                    _toastNotification.AddErrorToastMessage("Facility already exists", new NotyOptions() { Timeout = 5000 });
                    return View();
                }
                else
                {
                    mstFacilityEntityFromDB.ModifiedDate = DateTime.Now;
                    mstFacilityEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstFacilityEntityFromDB.Code = mstFacility.Code;
                    mstFacilityEntityFromDB.DepartmentID = mstFacility.DepartmentID;
                    mstFacilityEntityFromDB.UserID = mstFacility.UserID;
                    mstFacilityEntityFromDB.FacilityName = mstFacility.FacilityName;
                    mstFacilityEntityFromDB.IsActive = mstFacility.IsActive;

                    _repository.MstFacility.UpdateFacility(mstFacilityEntityFromDB);
                    await _repository.SaveAsync();
                    _toastNotification.AddSuccessToastMessage("Facility updated successfully", new NotyOptions() { Timeout = 5000 });
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateFacility action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Edit Facility. Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
