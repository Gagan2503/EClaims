using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class DtMileageClaimDraftRepository : RepositoryBase<DtMileageClaimDraft>, IDtMileageClaimDraftRepository
    {
        public DtMileageClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtMileageClaimDraft(EClaimsEntities.Models.DtMileageClaimDraft dtMileageClaim)
        {
            Create(dtMileageClaim);
        }

        public void DeleteDtMileageClaimDraft(EClaimsEntities.Models.DtMileageClaimDraft dtMileageClaim)
        {
            Delete(dtMileageClaim);
        }

        public async Task<IEnumerable<DtMileageClaimDraft>> GetAllDtMileageClaimDraftAsync()
        {
            return await FindAll()
             .OrderBy(mc => mc.MCItemID)
             .ToListAsync();
        }

        public async Task<IEnumerable<DtMileageClaimDraft>> GetAllDtMileageClaimDraftWithDetailsAsync()
        {
            return await FindAll()
                .Include(mf => mf.MstFacility)
                .ToListAsync();
        }

        public async Task<List<DtMileageClaimDraft>> GetDtMileageClaimDraftByIdAsync(long? mCID)
        {
            return await FindByCondition(dtMC => dtMC.MCID.Equals(mCID))
                 .OrderBy(dtMC => dtMC.OrderBy)
                 .Include(mf => mf.MstFacility)
                 .OrderBy(dtMC => dtMC.OrderBy)
                 .ToListAsync();
        }

        public void UpdateDtMileageClaimDraft(EClaimsEntities.Models.DtMileageClaimDraft dtMileageClaim)
        {
            throw new NotImplementedException();
        }
    }
}
