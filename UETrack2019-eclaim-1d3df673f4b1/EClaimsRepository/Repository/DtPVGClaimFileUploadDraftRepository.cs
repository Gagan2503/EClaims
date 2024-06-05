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
   public class DtPVGClaimFileUploadDraftRepository : RepositoryBase<DtPVGClaimFileUploadDraft>, IDtPVGClaimFileUploadDraftRepository
    
    {
        public DtPVGClaimFileUploadDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft)
        {
            Create(dtPVGClaimFileUploadDraft);
        }

        public void DeleteDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft)
        {
            Delete(dtPVGClaimFileUploadDraft);
        }

        public async Task<List<DtPVGClaimFileUploadDraft>> GetDtPVGClaimDraftAuditByIdAsync(long? pVGCID)
        {
            return FindByCondition(dtMC => dtMC.PVGCID.Equals(pVGCID)).ToList();
        }

        public async Task<DtPVGClaimFileUploadDraft> GetDtPVGClaimFileUploadDraftByIdAsync(long? pVGFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(pVGFID)).FirstOrDefault();
        }

        public void UpdateDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft)
        {
            Update(dtPVGClaimFileUploadDraft);
        }
    }
}
