#pragma checksum "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "008bb150b455f77027e7884a7bdefb38a3290bfc78100c82e7b2226c8e52f3d6"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_HRPVGiroClaim_GetHRPVGDetailsPrint), @"mvc.1.0.view", @"/Views/HRPVGiroClaim/GetHRPVGDetailsPrint.cshtml")]
namespace AspNetCore
{
    #line hidden
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Mvc;
    using global::Microsoft.AspNetCore.Mvc.Rendering;
    using global::Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\_ViewImports.cshtml"
using EClaimsWeb;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\_ViewImports.cshtml"
using EClaimsWeb.Models;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"008bb150b455f77027e7884a7bdefb38a3290bfc78100c82e7b2226c8e52f3d6", @"/Views/HRPVGiroClaim/GetHRPVGDetailsPrint.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"a4db12db2d232aa74afb152d1d99af05b5c5c936f2e6c6c148681d63b353a31b", @"/Views/_ViewImports.cshtml")]
    #nullable restore
    public class Views_HRPVGiroClaim_GetHRPVGDetailsPrint : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<EClaimsWeb.Models.HRPVGClaimDetailVM>
    #nullable disable
    {
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_HeadTagHelper;
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\n");
            WriteLiteral("\n");
#nullable restore
#line 4 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
  
    ViewBag.Title = "GetHRPVGPrint";
    Layout = null;
    int Counter = 1;

#line default
#line hidden
#nullable disable
            WriteLiteral("\n<!DOCTYPE html>\n<html>\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("head", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "008bb150b455f77027e7884a7bdefb38a3290bfc78100c82e7b2226c8e52f3d63931", async() => {
                WriteLiteral("\n    <style type=\"text/css\" media=\"print\">\n        ");
                WriteLiteral("@media print {\n\n            ");
                WriteLiteral("@page {\n                size: auto !important;\n                max-height: 100%;\n                max-width: 100%\n            }\n        }\n    </style>\n");
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_HeadTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_HeadTagHelper);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("body", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "008bb150b455f77027e7884a7bdefb38a3290bfc78100c82e7b2226c8e52f3d65229", async() => {
                WriteLiteral(@"

    <div style=""width:100%; text-align:center;""><h4>HR PV-GIRO Claim Details</h4></div>
    <br />

    <table class=""table table-bordered"">
        <thead>
            <tr>
                <th>Name</th>
                <th>Company</th>
                <th>Department</th>
                <th>Facility</th>
                <th>Date</th>
                <th>Payment Mode</th>
                <th>Voucher No</th>
                <th>Claim #</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><label id=""lblName"">");
#nullable restore
#line 44 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(Model.HRPVGClaimVM.Name);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblCompany\">");
#nullable restore
#line 45 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                      Write(Model.HRPVGClaimVM.Company);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblDepartment\">");
#nullable restore
#line 46 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                         Write(Model.HRPVGClaimVM.DepartmentName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblFacility\">");
#nullable restore
#line 47 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                       Write(Model.HRPVGClaimVM.FacilityName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblCurrentDate\">");
#nullable restore
#line 48 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                          Write(Model.HRPVGClaimVM.CreatedDate);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblCurrentDate\">");
#nullable restore
#line 49 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                          Write(Model.HRPVGClaimVM.PaymentMode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblVoucherNo\">");
#nullable restore
#line 50 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                        Write(Model.HRPVGClaimVM.VoucherNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblHRPVGCClaimNo\">");
#nullable restore
#line 51 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                            Write(Model.HRPVGClaimVM.HRPVGCNo);

#line default
#line hidden
#nullable disable
                WriteLiteral(@"</label></td>
            </tr>
        </tbody>
    </table>
    <div class=""row mt-4"">
        <h5 class=""col-md-12 card-title table-summary"">List Of Payee</h5>
        <div class=""col-md-12"">
            <div class=""table-responsive"">
                <table id=""order-listing"" class=""table table-bordered"">
                    <thead>
                        <tr>
                            <th>Item #</th>
                            <th>Payee Name</th>
                            <th>Particulars of Payment</th>
                            <th>Employee No</th>
                            <th>Facility Name</th>
");
#nullable restore
#line 67 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                             if (Model.HRPVGClaimVM.PaymentMode == "GIRO" || Model.HRPVGClaimVM.PaymentMode == "TT" || Model.HRPVGClaimVM.PaymentMode == "Fast Payment" || Model.HRPVGClaimVM.PaymentMode == "RTGS")
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <th>Bank</th>\n                                <th>Bank Code</th>\n                                <th>Branch Code</th>\n                                <th>Bank Account</th>\n");
#nullable restore
#line 73 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                            }
                            else
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <th>Mobile/UEN No</th>\n");
#nullable restore
#line 77 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                            }

#line default
#line hidden
#nullable disable
                WriteLiteral("                            <th>Amount</th>\n                            <th>Account Code</th>\n                        </tr>\n                    </thead>\n                    <tbody>\n");
#nullable restore
#line 83 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                         foreach (var item in Model.DtHRPVGClaimVMs)
                        {

#line default
#line hidden
#nullable disable
                WriteLiteral("                            <tr>\n                                <td>");
#nullable restore
#line 86 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(Counter);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
                WriteLiteral("                                <td>");
#nullable restore
#line 88 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(item.StaffName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 89 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(item.Reason);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 90 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(item.EmployeeNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 91 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(item.Facility);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
#nullable restore
#line 92 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                 if (Model.HRPVGClaimVM.PaymentMode == "GIRO" || Model.HRPVGClaimVM.PaymentMode == "TT" || Model.HRPVGClaimVM.PaymentMode == "Fast Payment" || Model.HRPVGClaimVM.PaymentMode == "RTGS")
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <td>");
#nullable restore
#line 94 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.Bank);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 95 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.BankCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 96 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.BranchCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 97 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.BankAccount);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
#nullable restore
#line 98 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <td>");
#nullable restore
#line 101 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.Mobile);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
#nullable restore
#line 102 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <td class=\"text-align-right\">$");
#nullable restore
#line 103 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                         Write(Math.Round((decimal)item.Amount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td class=\"tableAccountCode\">");
#nullable restore
#line 104 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                        Write(item.AccountCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                            </tr>\n");
#nullable restore
#line 106 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                            Counter++;
                        }

#line default
#line hidden
#nullable disable
                WriteLiteral(@"                        <tr>
                            <td></td>
                            <td></td>
                            <td></td>
                            <td></td>
                            <td></td>
                            <th>Grand Total</th>
                            <td class=""text-align-right"">$");
#nullable restore
#line 115 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                     Write(Math.Round((decimal)@Model.HRPVGClaimVM.GrandTotal, (int)3).ToString("#,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                        </tr>\n");
                WriteLiteral(@"                    </tbody>
                </table>
            </div>
        </div>
    </div>
    <div class=""row mt-4"">
        <h5 class=""col-md-12 card-title table-summary"">Summary of Accounts Allocation</h5>
        <div class=""col-md-10"">
            <div class=""table-responsive"">
                <table id=""order-listing"" class=""table table-bordered"">
                    <thead>
                        <tr>
                            <th>Account Code</th>
                            <th>Facility</th>
                            <th>Expense Type</th>
                            <th>Description</th>
                            <th>Tax Class</th>
                            <th>Amount</th>
                        </tr>
                    </thead>
                    <tbody>
");
#nullable restore
#line 149 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                         foreach (var item in Model.DtHRPVGClaimSummaries)
                        {
                            if (item.ExpenseCategory != "DBS")
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <tr>\n                                    <td>");
#nullable restore
#line 154 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.AccountCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 155 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.Facility);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 156 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.ExpenseCategory);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 157 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">");
#nullable restore
#line 158 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                            Write(Math.Round((decimal)item.TaxClass, (int)3).ToString("#,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 159 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.Amount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                </tr>\n");
#nullable restore
#line 161 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                            }
                            else
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <tr>\n                                    <td>");
#nullable restore
#line 165 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.AccountCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td></td>\n                                    <td>");
#nullable restore
#line 167 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.ExpenseCategory);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 168 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                   Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td></td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 170 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)@Model.HRPVGClaimVM.GrandTotal, (int)3).ToString("#,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                </tr>\n");
#nullable restore
#line 172 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                            }
                        }

#line default
#line hidden
#nullable disable
                WriteLiteral(@"                    </tbody>
                </table>
            </div>
        </div>
    </div>
    <div class=""row mt-4"">
        <h5 class=""col-md-12 card-title table-summary""></h5>
        <div class=""col-md-12 grid-margin stretch-card"">
            <div class=""card"">
                <div class=""card-body"">
                    <h4 class=""card-title"">
                        Audit Trail
                    </h4>
                    <ul class=""bullet-line-list pt-2 mb-0"">
");
#nullable restore
#line 188 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                         foreach (var itemAudit in Model.HRPVGClaimAudits)
                        {

#line default
#line hidden
#nullable disable
                WriteLiteral("                            <p>\n                                ");
#nullable restore
#line 191 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                           Write(itemAudit.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                            </p>\n                            <!--<li>\n");
#nullable restore
#line 194 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                 if (itemAudit.Action.ToString() == "1" && @itemAudit.Description.ToString().ToLower().Contains("created"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        New\n                                    </h6>\n");
#nullable restore
#line 199 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "1" || itemAudit.Action.ToString() == "2") && @itemAudit.Description.ToString().ToLower().Contains("verification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Awaiting verification\n                                    </h6>\n");
#nullable restore
#line 205 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "2" || itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("approval"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Awaiting Signatory approval\n                                    </h6>\n");
#nullable restore
#line 211 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Approved\n                                    </h6>\n");
#nullable restore
#line 217 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("approved"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Approved\n                                    </h6>\n");
#nullable restore
#line 223 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "4") && @itemAudit.Description.ToString().ToLower().Contains("reject"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Rejected\n                                    </h6>\n");
#nullable restore
#line 229 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "4") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Rejected\n                                    </h6>\n");
#nullable restore
#line 235 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "-5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Requested to Void\n                                    </h6>\n");
#nullable restore
#line 241 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "-5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Requested to Void\n                                    </h6>\n");
#nullable restore
#line 247 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 253 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 259 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 265 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "0") && @itemAudit.Description.ToString().ToLower().Contains("query"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Query\n                                    </h6>\n");
#nullable restore
#line 271 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "1" || itemAudit.Action.ToString() == "2") && @itemAudit.Description.ToString().ToLower().Contains("amended by"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Amended\n                                    </h6>\n");
#nullable restore
#line 277 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                                }

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <p>\n                                    ");
#nullable restore
#line 279 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(itemAudit.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                                </p>\n                                <p class=\"text-muted mb-3 tx-12\">\n                                    <i class=\"mdi mdi-clock-outline\"></i>\n                                    ");
#nullable restore
#line 283 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                               Write(itemAudit.AuditDateTickle);

#line default
#line hidden
#nullable disable
                WriteLiteral("-->\n");
                WriteLiteral("                                                <!--</p>\n                            </li>-->\n");
#nullable restore
#line 287 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HRPVGiroClaim\GetHRPVGDetailsPrint.cshtml"
                        }

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                    </ul>\n                </div>\n            </div>\n        </div>\n        <div class=\"col-md-4 grid-margin stretch-card\">\n\n        </div>\n    </div>\n");
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\n</html>\n");
        }
        #pragma warning restore 1998
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<EClaimsWeb.Models.HRPVGClaimDetailVM> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591