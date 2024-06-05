﻿using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstScreensRepository : IRepositoryBase<MstScreens>
    {
        Task<IEnumerable<MstScreens>> GetAllScreensAsync();
        IEnumerable<MstScreens> GetAllScreensByModuleAsync(string moduleName);
    }
}