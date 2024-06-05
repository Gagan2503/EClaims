using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstHRPVCClaimDraftRepository: IRepositoryBase<MstHRPVCClaimDraft>
    {

        Task<MstHRPVCClaimDraft> GetHRPVCClaimByIdAsync(long? hRPVCCID);

        void DeleteHRPVCClaim(MstHRPVCClaimDraft mstHRPVCClaim);
        
    }
}
