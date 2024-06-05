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
    public class DtHRPVCClaimFileUploadRepository : RepositoryBase<DtHRPVCClaimFileUpload>, IDtHRPVCClaimFileUploadRepository
    {
        public DtHRPVCClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload)
        {
            Create(dtHRPVCClaimFileUpload);
        }


        public void DeleteDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload)
        {
            Delete(dtHRPVCClaimFileUpload);
        }

       
        public async Task<List<DtHRPVCClaimFileUpload>> GetDtHRPVCClaimAuditByIdAsync(long? hRPVCCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVCCID.Equals(hRPVCCID)).ToList();
        }

        public async Task<DtHRPVCClaimFileUpload> GetDtHRPVCClaimFileUploadByIdAsync(long? hRPVCFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(hRPVCFID)).FirstOrDefault();
        }

        public void UpdateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload)
        {
            Update(dtHRPVCClaimFileUpload);
        }

    }
}
