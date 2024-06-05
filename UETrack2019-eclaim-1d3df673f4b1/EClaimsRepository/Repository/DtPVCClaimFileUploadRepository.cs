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
    public class DtPVCClaimFileUploadRepository : RepositoryBase<DtPVCClaimFileUpload>, IDtPVCClaimFileUploadRepository
    {
        public DtPVCClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
public void CreateDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload)
        {
            Create(dtPVCClaimFileUpload);
        }

        public void DeleteDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload)
        {
            Delete(dtPVCClaimFileUpload);
        }

        public async Task<List<DtPVCClaimFileUpload>> GetDtPVCClaimAuditByIdAsync(long? pVCCID)
        {
            return FindByCondition(dtMC => dtMC.PVCCID.Equals(pVCCID)).ToList();
        }

        public async Task<DtPVCClaimFileUpload> GetDtPVCClaimFileUploadByIdAsync(long? pVCFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(pVCFID)).FirstOrDefault();
        }

        public void UpdateDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload)
        {
            Update(dtPVCClaimFileUpload);
        }

    }
}
