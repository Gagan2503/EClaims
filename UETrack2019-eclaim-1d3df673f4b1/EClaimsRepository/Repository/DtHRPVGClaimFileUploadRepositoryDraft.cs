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
    public class DtHRPVGClaimFileUploadRepositoryDraft : RepositoryBase<DtHRPVGClaimFileUploadDraft>, IDtHRPVGClaimFileUploadRepositoryDraft
    {
        public DtHRPVGClaimFileUploadRepositoryDraft(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload)
        {
            Create(dtHRPVGClaimFileUpload);
        }

        public void DeleteDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload)
        {
            Delete(dtHRPVGClaimFileUpload);
        }

        public async Task<List<DtHRPVGClaimFileUploadDraft>> GetDtHRPVGClaimAuditByIdAsync(long? hRPVGCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVGCID.Equals(hRPVGCID)).ToList();
        }

        public async Task<DtHRPVGClaimFileUploadDraft> GetDtHRPVGClaimFileUploadByIdAsync(long? hRPVGFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(hRPVGFID)).FirstOrDefault();
        }


        public void UpdateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload)
        {
            Update(dtHRPVGClaimFileUpload);
        }

    }
}
