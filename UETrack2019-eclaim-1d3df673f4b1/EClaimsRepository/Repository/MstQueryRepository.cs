using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstQueryRepository : RepositoryBase<MstQuery>, IMstQueryRepository
    {
        public MstQueryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task CreateQuery(MstQuery mstQuery)
        {
            Create(mstQuery);
        }

        public void DeleteQuery(MstQuery mstQuery)
        {
            Delete(mstQuery);
        }

        public void UpdateQuery(MstQuery mstQuery)
        {
            Update(mstQuery);
        }

        public async Task<List<MstQuery>> GetAllClaimsQueriesAsync(int? UserId,long? Mcid,string Module)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllClaimsQueries";
            var parameters = new DynamicParameters();
            parameters.Add("MCID", Mcid, DbType.Int64, ParameterDirection.Input);
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("ModuleType", Module, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            { 
                var queries = await RepositoryContext.Connection.QueryAsync<MstQuery>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
                return queries.ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                RepositoryContext.Connection.Close();
            }
        }
    }
}
