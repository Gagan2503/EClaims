using EClaimsEntities;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EClaimsEntities.Models;
using System.Globalization;

namespace EClaimsWeb.Helpers
{
    public class DelegateUserHelper
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly RepositoryContext _context;
        CultureInfo culture = new CultureInfo("es-ES");

        public DelegateUserHelper(ILoggerManager logger, IRepositoryWrapper repository, RepositoryContext context)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
        }

        public DelegateUserHelper(ILoggerManager logger, IRepositoryWrapper repository)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Nullable<int>> IsDelegateUserSetForUser(int userId)
        {
            int? delegateUserId = null;
            // Get approval configurations from MstDelegateUsers
            MstDelegateUsers delUserSettings = await _repository.MstDelegateUsers.GetDelegateUserByUserIdAsync(userId);
            //MstAlternateApprovers altAprroverSettings = await _repository.MstAlternateApprover.GetAlternateApproverByUserIdAsync(userId);
            if (delUserSettings != null)
            {
                // Check delegate user is set and enabled for the current user
                if (delUserSettings.IsActive)
                {
                    DateTime currentDate = DateTime.Now;
                    DateTime fromDate = delUserSettings.FromDate;
                    DateTime toDate = delUserSettings.ToDate;
                    if (currentDate.Ticks > fromDate.Ticks && currentDate.Ticks < toDate.Ticks)
                    {
                        return Convert.ToInt32(delUserSettings.DelegateUser);
                    }
                }
            }
            return delegateUserId;
        }

        public async Task<Nullable<int>> IsUserHasAnyDelegateUserSet(int loggedInUserId)
        {
            long loggedInUser = Convert.ToInt64(loggedInUserId);
            int? delegateUserId = null;
            // Get approval configurations from MstDelegateUsers
            List<MstDelegateUsers> delUserSettings = _repository.MstDelegateUsers.FindByCondition(x => x.DelegateUser == loggedInUser).ToList();
            //List<MstAlternateApprovers> altAprroverSettings = _repository.MstAlternateApprover.FindByCondition(x => x.AlternateUser == loggedInUser).ToList();
            if (delUserSettings != null)
            {
                foreach (var item in delUserSettings)
                {
                    // Check delegate user is set and enabled for the current user
                    if (item.IsActive)
                    {
                        DateTime currentDate = DateTime.Now;
                        DateTime fromDate = item.FromDate;
                        DateTime toDate = item.ToDate;
                        if (currentDate.Ticks > fromDate.Ticks && currentDate.Ticks < toDate.Ticks)
                        {
                            return Convert.ToInt32(item.UserId);
                        }
                    }
                }
            }
            return delegateUserId;
        }
    }
}
