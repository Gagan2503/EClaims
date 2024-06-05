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
    public class AlternateApproverHelper
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly RepositoryContext _context;
        CultureInfo culture = new CultureInfo("es-ES");

        public AlternateApproverHelper(ILoggerManager logger, IRepositoryWrapper repository, RepositoryContext context)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
        }

        public async Task<Nullable<int>> IsAlternateApprovalSetForUser(int userId)
        {
            int? alternateUserId = null;
            // Get approval configurations from MstAlternateApprovers
            MstAlternateApprovers altAprroverSettings = await _repository.MstAlternateApprover.GetAlternateApproverByUserIdAsync(userId);
            if (altAprroverSettings != null)
            {
                // Check alternate approval is set and enabled for the current user
                if (altAprroverSettings.IsActive)
                {
                    DateTime currentDate = DateTime.Now;
                    DateTime fromDate = altAprroverSettings.FromDate;
                    DateTime toDate = altAprroverSettings.ToDate;
                    if (currentDate.Ticks > fromDate.Ticks && currentDate.Ticks < toDate.Ticks)
                    {
                        return Convert.ToInt32(altAprroverSettings.AlternateUser);
                    }
                }
            }
            return alternateUserId;
        }

        public async Task<Nullable<int>> IsUserHasAnyAlternateApprovalSet(int loggedInUserId)
        {
            long loggedInUser = Convert.ToInt64(loggedInUserId);
            int? alternateUserId = null;
            // Get approval configurations from MstAlternateApprovers
            List<MstAlternateApprovers> altAprroverSettings = _repository.MstAlternateApprover.FindByCondition(x => x.AlternateUser == loggedInUser).ToList();
            if (altAprroverSettings != null)
            {
                foreach (var item in altAprroverSettings)
                {
                    // Check alternate approval is set and enabled for the current user
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
            return alternateUserId;
        }
    }
}
