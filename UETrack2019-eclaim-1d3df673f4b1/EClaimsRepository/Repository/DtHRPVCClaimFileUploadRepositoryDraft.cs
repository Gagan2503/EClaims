using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class DtHRPVCClaimFileUploadRepositoryDraft : RepositoryBase<DtHRPVCClaimFileUploadDraft>, IDtHRPVCClaimFileUploadRepositoryDraft
    {
        public DtHRPVCClaimFileUploadRepositoryDraft(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUploadDraft dtHRPVCClaimFileUpload)
        {
            Create(dtHRPVCClaimFileUpload);
        }

        public async Task<List<DtHRPVCClaimFileUploadDraft>> GetDtHRPVCClaimAuditByIdAsync(long? hRPVCCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVCCID.Equals(hRPVCCID)).ToList();
        }

    }
}
