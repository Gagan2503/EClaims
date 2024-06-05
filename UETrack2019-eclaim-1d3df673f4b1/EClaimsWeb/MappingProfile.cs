using AutoMapper;
using EClaimsEntities.DataTransferObjects;
using EClaimsEntities.Models;
using EClaimsWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MstDepartment, DepartmentViewModel>();
            CreateMap<MstTaxClass, TaxClassViewModel>();
            CreateMap<TaxClassViewModel, MstTaxClass>();
            CreateMap<DepartmentViewModel, MstDepartment>();
            CreateMap<MstUser, LoginViewModel>();
            CreateMap<LoginViewModel,MstUser>();
            CreateMap<MstApprovalMatrix, MstApprovalMatrix>();
            CreateMap<MstUser, UserViewModel>();
            CreateMap<UserViewModel, MstUser>();
            CreateMap<BankDetailsViewModel, MstBankDetails>();
            CreateMap<MstBankDetails, BankDetailsViewModel>();
            CreateMap<UserApproversViewModel, MstUserApprovers>();
            CreateMap<MstUserApprovers, UserApproversViewModel>();
            CreateMap<MstUser , UserVM>();
            CreateMap<UserVM, MstUser>();
        }
    }
}
