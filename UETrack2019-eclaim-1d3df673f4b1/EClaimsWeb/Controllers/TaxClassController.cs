using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
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
    public class TaxClassController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;

        public TaxClassController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var mstTaxClass = await _repository.MstTaxClass.GetAllTaxClassDataAsync();
                _logger.LogInfo($"Returned all tax class from database.");

                var mstTaxClassResult = _mapper.Map<IEnumerable<TaxClassViewModel>>(mstTaxClass);

                var IsOptionalSelectedItems = (from a in mstTaxClass
                                               from b in mstTaxClass.Where(x => x.TaxClassID == a.OptionalTaxClassID).DefaultIfEmpty()
                                               where (a.IsDefault == true && a.IsOptional == true)
                                               select new
                                               {
                                                   b.TaxClass
                                               }).ToList().AsEnumerable();
                foreach(var item in mstTaxClassResult)
                {
                    if (item.IsOptional)
                    {
                        foreach(var isoptionalitem in IsOptionalSelectedItems)
                        {
                            item.OptionalTaxClass = isoptionalitem.TaxClass;
                        }
                    }
                }

                return View(mstTaxClassResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllTaxClassAsync action: {ex.Message}");
                return View();
            }
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxClassViewModel mstTaxClass)
        {
            try
            {
                if (mstTaxClass == null)
                {
                    _logger.LogError("Tax Class object sent from client is null.");
                    return BadRequest("Tax Class object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid tax class while adding. Error = " + modelErrors);
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return View(mstTaxClass);
                }

                var mstTaxClassEntity = _mapper.Map<MstTaxClass>(mstTaxClass);

                if (_repository.MstTaxClass.ValidateTaxClass(mstTaxClassEntity, "create"))
                {
                    _toastNotification.AddErrorToastMessage("Tax Class already exists", new NotyOptions() { Timeout = 5000 });
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return View();
                }
                else
                {
                    mstTaxClass.CreatedDate = DateTime.Now;
                    //mstTaxClass.ModifiedDate = DateTime.Now;
                    mstTaxClass.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    //mstTaxClass.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    //mstTaxClass. = DateTime.Now;
                    //mstTaxClass.ApprovalStatus = 3;
                    //mstTaxClass.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    mstTaxClassEntity = _mapper.Map<MstTaxClass>(mstTaxClass);

                    var entity = await _repository.MstTaxClass.CreateAndReturnTaxClass(mstTaxClassEntity);

                    await _repository.SaveAsync();

                   // await _repository.MstTaxClass.InsertApprovalMatrixForTaxClass(entity.TaxClassID, Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                   // await _repository.SaveAsync();

                    var createdTaxClass = _mapper.Map<TaxClassViewModel>(mstTaxClassEntity);
                }
                _toastNotification.AddSuccessToastMessage("Tax Class added successfully", new NotyOptions() { Timeout = 5000 });
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateTaxClass action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Add Tax Class. Error: {ex.Message}");
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

            var mstTaxClass = await _repository.MstTaxClass.GetTaxClassByIdAsync(id);
            var createdTaxClass = _mapper.Map<TaxClassViewModel>(mstTaxClass);
            if (mstTaxClass == null)
            {
                return NotFound();
            }
            SelectList listItems = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
            ViewData["TaxClass"] = listItems.Where(item => item.Value != id.ToString());
            return View(createdTaxClass);
        }

        [Authorize(Roles = "Admin")]
        // POST: Facility/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, TaxClassViewModel mstTaxClass)
        {
            try
            {
                if (mstTaxClass == null)
                {
                    _logger.LogError("Facility object sent from client is null.");
                    return BadRequest("Facility object is null");
                }

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid Tax Class while add. Error = " + modelErrors);
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return View(mstTaxClass);
                }

                var mstTaxClassEntityFromDB = await _repository.MstTaxClass.GetTaxClassByIdAsync(id);
                if (mstTaxClassEntityFromDB == null)
                {
                    string errorMessage = $"Tax Class with id: {id}, hasn't been found in db.";
                    _logger.LogError(errorMessage);
                    _toastNotification.AddErrorToastMessage(errorMessage);
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return NotFound();
                }

                var mstTaxClassEntityMod = _mapper.Map<MstTaxClass>(mstTaxClass);

                if (mstTaxClassEntityFromDB.Code == mstTaxClassEntityMod.Code && mstTaxClassEntityFromDB.TaxClass == mstTaxClassEntityMod.TaxClass)
                {
                    mstTaxClassEntityFromDB.ModifiedDate = DateTime.Now;
                    mstTaxClassEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstTaxClassEntityFromDB.Code = mstTaxClassEntityMod.Code;
                    mstTaxClassEntityFromDB.TaxClass = mstTaxClassEntityMod.TaxClass;
                    mstTaxClassEntityFromDB.IsDefault = mstTaxClassEntityMod.IsDefault;
                    mstTaxClassEntityFromDB.Description = mstTaxClassEntityMod.Description;
                    mstTaxClassEntityFromDB.IsOptional = mstTaxClassEntityMod.IsOptional;
                    mstTaxClassEntityFromDB.OptionalTaxClassID = mstTaxClassEntityMod.OptionalTaxClassID;


                    await _repository.MstTaxClass.UpdateAndReturnTaxClass(mstTaxClassEntityFromDB);
                    await _repository.SaveAsync();

                    _toastNotification.AddSuccessToastMessage("Tax Class updated successfully", new NotyOptions() { Timeout = 5000 });
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return RedirectToAction("Index");

                }
                else if (_repository.MstTaxClass.ValidateTaxClass(mstTaxClassEntityFromDB, "edit"))
                {
                    _toastNotification.AddErrorToastMessage("Tax Class already exists", new NotyOptions() { Timeout = 5000 });
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return View();
                }
                else
                {
                    //mstTaxClassEntityFromDB.ModifiedDate = DateTime.Now;
                    //mstTaxClassEntityFromDB.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    mstTaxClassEntityFromDB.Code = mstTaxClassEntityMod.Code;
                    mstTaxClassEntityFromDB.TaxClass = mstTaxClassEntityMod.TaxClass;
                    //mstTaxClassEntityFromDB.IsDefault = mstTaxClassEntityMod.IsDefault;

                    await _repository.MstTaxClass.UpdateAndReturnTaxClass(mstTaxClassEntityFromDB);
                    await _repository.SaveAsync();
                    _toastNotification.AddSuccessToastMessage("Tax Class updated successfully");
                    ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateTaxClass action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Edit Tax Class. Error: {ex.Message}");
                ViewData["TaxClass"] = new SelectList(await _repository.MstTaxClass.GetAllTaxClassAsync("active"), "TaxClassID", "TaxClass");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
