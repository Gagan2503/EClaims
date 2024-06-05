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
    public class DtHRPVGClaimFileUploadRepository : RepositoryBase<DtHRPVGClaimFileUpload>, IDtHRPVGClaimFileUploadRepository
    {
        public DtHRPVGClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload)
        {
            Create(dtHRPVGClaimFileUpload);
        }

        public void DeleteDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload)
        {
            Delete(dtHRPVGClaimFileUpload);
        }

        public async Task<List<DtHRPVGClaimFileUpload>> GetDtHRPVGClaimAuditByIdAsync(long? hRPVGCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVGCID.Equals(hRPVGCID)).ToList();
        }

        public async Task<DtHRPVGClaimFileUpload> GetDtHRPVGClaimFileUploadByIdAsync(long? hRPVGFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(hRPVGFID)).FirstOrDefault();
        }


        public void UpdateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload)
        {
            Update(dtHRPVGClaimFileUpload);
        }

    }
}
