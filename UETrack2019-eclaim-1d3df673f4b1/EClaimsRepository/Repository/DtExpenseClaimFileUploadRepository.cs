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
    public class DtExpenseClaimFileUploadRepository : RepositoryBase<DtExpenseClaimFileUpload>, IDtExpenseClaimFileUploadRepository
    {
        public DtExpenseClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
public void CreateDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload)
        {
            Create(dtExpenseClaimFileUpload);
        }

        public void DeleteDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload)
        {
            Delete(dtExpenseClaimFileUpload);
        }

        public async Task<List<DtExpenseClaimFileUpload>> GetDtExpenseClaimAuditByIdAsync(long? eCID)
        {
            return FindByCondition(dtMC => dtMC.ECID.Equals(eCID)).ToList();
        }

        public async Task<DtExpenseClaimFileUpload> GetDtExpenseClaimFileUploadByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }

        public void UpdateDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload)
        {
            Update(dtExpenseClaimFileUpload);
        }

    }
}
