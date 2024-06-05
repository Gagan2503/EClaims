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
    public class DtTBClaimFileUploadRepository : RepositoryBase<DtTBClaimFileUpload>, IDtTBClaimFileUploadRepository
    {
        public DtTBClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload)
        {
            Create(dtTBClaimFileUpload);
        }

        public void DeleteDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload)
        {
            Delete(dtTBClaimFileUpload);
        }

        public void UpdateDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload)
        {
            Update(dtTBClaimFileUpload);
        }

        public async Task<List<DtTBClaimFileUpload>> GetDtTBClaimAuditByIdAsync(long? tBCID)
        {
            return FindByCondition(dtMC => dtMC.TBCID.Equals(tBCID)).ToList();
        }

        public async Task<DtTBClaimFileUpload> GetDtTBClaimFileUploadByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }
    }
}