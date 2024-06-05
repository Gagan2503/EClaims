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
    public class DtPVGClaimFileUploadRepository : RepositoryBase<DtPVGClaimFileUpload>, IDtPVGClaimFileUploadRepository
    {
        public DtPVGClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
public void CreateDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload)
        {
            Create(dtPVGClaimFileUpload);
        }

        public void DeleteDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload)
        {
            Delete(dtPVGClaimFileUpload);
        }

        public async Task<List<DtPVGClaimFileUpload>> GetDtPVGClaimAuditByIdAsync(long? pVGCID)
        {
            return FindByCondition(dtMC => dtMC.PVGCID.Equals(pVGCID)).ToList();
        }

        public async Task<DtPVGClaimFileUpload> GetDtPVGClaimFileUploadByIdAsync(long? pVGFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(pVGFID)).FirstOrDefault();
        }

        public void UpdateDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload)
        {
            Update(dtPVGClaimFileUpload);
        }

    }
}
