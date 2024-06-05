using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstPVCClaimDraftRepository:IRepositoryBase<MstPVCClaimDraft>
    {
        Task<List<CustomClaim>> GetAllPVCClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstPVCClaimDraft>> GetAllPVCClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID);
        Task<int> SaveItemsDraft(MstPVCClaimDraft mstPVCClaimDraft, List<DtPVCClaimDraft> dtPVCClaimsDraft, List<DtPVCClaimSummaryDraft> dtPVCClaimSummariesDraft);
        Task<MstPVCClaimDraft> GetPVCClaimDraftByIdAsync(long? pVCCID);

        void DeletePVCClaimDraft(MstPVCClaimDraft mstPVCClaim);

    }
}
