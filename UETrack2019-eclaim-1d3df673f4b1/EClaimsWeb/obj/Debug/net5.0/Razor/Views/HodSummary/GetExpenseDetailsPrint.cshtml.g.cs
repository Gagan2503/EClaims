#pragma checksum "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "e3cd43ec669b10c3201f1957e60da07a397b95f0c0b65cee75ae179217759e20"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_HodSummary_GetExpenseDetailsPrint), @"mvc.1.0.view", @"/Views/HodSummary/GetExpenseDetailsPrint.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"e3cd43ec669b10c3201f1957e60da07a397b95f0c0b65cee75ae179217759e20", @"/Views/HodSummary/GetExpenseDetailsPrint.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"a4db12db2d232aa74afb152d1d99af05b5c5c936f2e6c6c148681d63b353a31b", @"/Views/_ViewImports.cshtml")]
    #nullable restore
    public class Views_HodSummary_GetExpenseDetailsPrint : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<EClaimsWeb.Models.ExpenseClaimDetailVM>
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
#line 4 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
  
    ViewBag.Title = "GetExpensePrint";
    Layout = null;
    int Counter = 1;

#line default
#line hidden
#nullable disable
            WriteLiteral("\n<!DOCTYPE html>\n<html>\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("head", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "e3cd43ec669b10c3201f1957e60da07a397b95f0c0b65cee75ae179217759e203929", async() => {
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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("body", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "e3cd43ec669b10c3201f1957e60da07a397b95f0c0b65cee75ae179217759e205227", async() => {
                WriteLiteral(@"

    <div style=""width:100%; text-align:center;""><h4>Expense Claim Details</h4></div>
    <br />

    <table class=""table table-bordered"">
        <thead>
            <tr>
                <th>Name</th>
                <th>Company</th>
                <th>Department</th>
                <th>Facility</th>
                <th>Date</th>
                <th>Claim Type</th>
                <th>Voucher No</th>
                <th>Claim #</th>
");
                WriteLiteral("            </tr>\n        </thead>\n        <tbody>\n            <tr>\n                <td><label id=\"lblName\">");
#nullable restore
#line 45 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(Model.ExpenseClaimVM.Name);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblCompany\">");
#nullable restore
#line 46 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                      Write(Model.ExpenseClaimVM.Company);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblDepartment\">");
#nullable restore
#line 47 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                         Write(Model.ExpenseClaimVM.DepartmentName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblFacility\">");
#nullable restore
#line 48 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                       Write(Model.ExpenseClaimVM.FacilityName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblCurrentDate\">");
#nullable restore
#line 49 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                          Write(Model.ExpenseClaimVM.CreatedDate);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblClaimType\">");
#nullable restore
#line 50 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                        Write(Model.ExpenseClaimVM.ClaimType);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblVoucherNo\">");
#nullable restore
#line 51 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                        Write(Model.ExpenseClaimVM.VoucherNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n                <td><label id=\"lblECClaimNo\">");
#nullable restore
#line 52 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                        Write(Model.ExpenseClaimVM.ECNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</label></td>\n");
                WriteLiteral(@"            </tr>
        </tbody>
    </table>
    <div class=""row mt-4"">
        <h5 class=""col-md-12 card-title table-summary"">Claim Details</h5>
        <div class=""col-md-12"">
            <div class=""table-responsive"">
                <table id=""order-listing"" class=""table table-bordered"">
                    <thead>
                        <tr>
                            <th>Item #</th>
                            <th>Date</th>
                            <th>Facility</th>
                            <th>Description of Expense</th>
                            <th>Expense Category</th>
                            <th>Amount</th>
                            <th>GST</th>
                            <th>Total Amount With GST</th>
                        </tr>
                    </thead>
                    <tbody>
");
#nullable restore
#line 77 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                         foreach (var item in Model.DtExpenseClaimVMs)
                        {

#line default
#line hidden
#nullable disable
                WriteLiteral("                            <tr>\n                                <td>");
#nullable restore
#line 80 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(Counter);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 81 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(Convert.ToDateTime(item.DateOfJourney).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 82 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(item.Facility);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 83 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td>");
#nullable restore
#line 84 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(item.ExpenseCategory);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td class=\"text-align-right\">$");
#nullable restore
#line 85 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                         Write(Math.Round((decimal)item.Amount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                <td class=\"text-align-right\">$");
#nullable restore
#line 86 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                         Write(Math.Round((decimal)item.Gst, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("%</td>\n                                <td class=\"text-align-right\">$");
#nullable restore
#line 87 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                         Write(Math.Round((decimal)item.AmountWithGST, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                            </tr>\n");
#nullable restore
#line 89 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                            Counter++;
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
        <h5 class=""col-md-12 card-title table-summary"">Summary of Accounts Allocation</h5>
        <div class=""col-md-10"">
            <div class=""table-responsive"">
                <table id=""order-listing"" class=""table table-bordered"">
                    <thead>
                        <tr>
                            <th>Account Code</th>
                            <th>Expense Type</th>
                            <th>Description</th>
                            <th>Amount (Excluding GST)</th>
                            <th>GST</th>
                            <th>Amount (Including GST)</th>
                        </tr>
                    </thead>
                    <tbody>
");
#nullable restore
#line 126 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                         foreach (var item in Model.DtExpenseClaimSummaries)
                        {
                            if (item.ExpenseCategory != "DBS")
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <tr>\n                                    <td>");
#nullable restore
#line 131 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.AccountCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 132 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.ExpenseCategory);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 133 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 134 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.Amount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 135 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.GST, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 136 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.AmountWithGST, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                </tr>\n");
#nullable restore
#line 138 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                            }
                            else
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <tr>\n                                    <td>");
#nullable restore
#line 142 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.AccountCode);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 143 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.ExpenseCategory);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td>");
#nullable restore
#line 144 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                   Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 145 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.Amount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 146 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.GST, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                    <td class=\"text-align-right\">$");
#nullable restore
#line 147 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                                             Write(Math.Round((decimal)item.AmountWithGST, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                                </tr>\n");
#nullable restore
#line 149 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
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
#line 165 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                         foreach (var itemAudit in Model.ExpenseClaimAudits)
                        {

#line default
#line hidden
#nullable disable
                WriteLiteral("                            <p>\n                                ");
#nullable restore
#line 168 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                           Write(itemAudit.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                            </p>\n                            <!--<li>\n");
#nullable restore
#line 171 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                 if (itemAudit.Action.ToString() == "1" && @itemAudit.Description.ToString().ToLower().Contains("created"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        New\n                                    </h6>\n");
#nullable restore
#line 176 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "1" || itemAudit.Action.ToString() == "2") && @itemAudit.Description.ToString().ToLower().Contains("verification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Awaiting verification\n                                    </h6>\n");
#nullable restore
#line 182 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "2" || itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("approval"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Awaiting Signatory approval\n                                    </h6>\n");
#nullable restore
#line 188 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Approved\n                                    </h6>\n");
#nullable restore
#line 194 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "3") && @itemAudit.Description.ToString().ToLower().Contains("approved"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Approved\n                                    </h6>\n");
#nullable restore
#line 200 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "4") && @itemAudit.Description.ToString().ToLower().Contains("reject"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Rejected\n                                    </h6>\n");
#nullable restore
#line 206 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "4") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Rejected\n                                    </h6>\n");
#nullable restore
#line 212 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "-5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Requested to Void\n                                    </h6>\n");
#nullable restore
#line 218 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "-5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Requested to Void\n                                    </h6>\n");
#nullable restore
#line 224 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 230 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("void"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 236 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "5") && @itemAudit.Description.ToString().ToLower().Contains("notification"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Voided\n                                    </h6>\n");
#nullable restore
#line 242 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }
                                else if ((itemAudit.Action.ToString() == "0") && @itemAudit.Description.ToString().ToLower().Contains("query"))
                                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                    <h6>\n                                        Query\n                                    </h6>\n");
#nullable restore
#line 248 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                                }

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <p>\n                                    ");
#nullable restore
#line 250 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(itemAudit.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                                </p>\n                                <p class=\"text-muted mb-3 tx-12\">\n                                    <i class=\"mdi mdi-clock-outline\"></i>\n                                    ");
#nullable restore
#line 254 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                               Write(itemAudit.AuditDateTickle);

#line default
#line hidden
#nullable disable
                WriteLiteral("-->\n");
                WriteLiteral("                                <!--</p>\n                            </li>-->\n");
#nullable restore
#line 258 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\HodSummary\GetExpenseDetailsPrint.cshtml"
                        }

#line default
#line hidden
#nullable disable
                WriteLiteral("                        \n                    </ul>\n                </div>\n            </div>\n        </div>\n        <div class=\"col-md-4 grid-margin stretch-card\">\n\n        </div>\n    </div>\n");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<EClaimsWeb.Models.ExpenseClaimDetailVM> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591