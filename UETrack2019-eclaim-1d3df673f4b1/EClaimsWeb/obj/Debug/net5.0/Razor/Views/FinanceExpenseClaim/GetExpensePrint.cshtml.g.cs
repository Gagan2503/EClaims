#pragma checksum "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "8f4aeed4a3bb6d782d034b4f513c5f27ed5818acceba1d9a6652ee86c44bac65"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_FinanceExpenseClaim_GetExpensePrint), @"mvc.1.0.view", @"/Views/FinanceExpenseClaim/GetExpensePrint.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"8f4aeed4a3bb6d782d034b4f513c5f27ed5818acceba1d9a6652ee86c44bac65", @"/Views/FinanceExpenseClaim/GetExpensePrint.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"a4db12db2d232aa74afb152d1d99af05b5c5c936f2e6c6c148681d63b353a31b", @"/Views/_ViewImports.cshtml")]
    #nullable restore
    public class Views_FinanceExpenseClaim_GetExpensePrint : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<List<EClaimsWeb.Models.ExpenseClaimVM>>
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
#line 4 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
  
    ViewBag.Title = "GetExpensePrint";
    Layout = null;

#line default
#line hidden
#nullable disable
            WriteLiteral("\n<!DOCTYPE html>\n<html>\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("head", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "8f4aeed4a3bb6d782d034b4f513c5f27ed5818acceba1d9a6652ee86c44bac653920", async() => {
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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("body", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "8f4aeed4a3bb6d782d034b4f513c5f27ed5818acceba1d9a6652ee86c44bac655218", async() => {
                WriteLiteral(@"

    <div style=""width:100%; text-align:center;""><h4>Expense Claims</h4></div>
    <br />

    <table class=""print-friendly"" width=""100%"" border=""0"" style=""font-size:10px; text-align:center;"">

        <tr style=""line-height:14px"">
            <td>Claim</td>
            <td>Voucher No</td>
            <td>Description</td>
            <td>Requester</td>
            <td>Date Created</td>
            <td>Facility</td>
            <td>Payee</td>
            <td>Contact Number</td>
            <td>Total Claim</td>
            <td>Approver</td>
            <td>Status</td>
        </tr>
        <tbody>
");
#nullable restore
#line 44 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
             foreach (var item in Model)
            {

#line default
#line hidden
#nullable disable
                WriteLiteral("            <tr style=\"line-height:13px\">\n");
                WriteLiteral("                <td>");
#nullable restore
#line 48 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.ECNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 49 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.VoucherNo);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 50 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.Description);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 51 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.Name);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
                WriteLiteral("                <td>");
#nullable restore
#line 53 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.CreatedDate);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 54 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.FacilityName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 55 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.Name);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                <td>");
#nullable restore
#line 56 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.Phone);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
                WriteLiteral("            <td class=\"text-align-right\">$");
#nullable restore
#line 62 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
                                     Write(Math.Round((decimal)item.TotalAmount, (int)2).ToString("###,##,##0.00"));

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n                \n");
#nullable restore
#line 64 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
                 if (item.ApprovalStatus != 3 && item.ApprovalStatus != 4 && item.ApprovalStatus != -5 && item.ApprovalStatus != 5)
                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                    <td>");
#nullable restore
#line 66 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
                   Write(item.Approver);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n");
#nullable restore
#line 67 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
                }
                else
                {

#line default
#line hidden
#nullable disable
                WriteLiteral("                    <td></td>\n");
#nullable restore
#line 71 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
                }

#line default
#line hidden
#nullable disable
                WriteLiteral("                <td>");
#nullable restore
#line 72 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
               Write(item.ExpenseStatusName);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\n            </tr>\n");
#nullable restore
#line 74 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\FinanceExpenseClaim\GetExpensePrint.cshtml"
            }

#line default
#line hidden
#nullable disable
                WriteLiteral("        </tbody>\n    </table>\n");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<List<EClaimsWeb.Models.ExpenseClaimVM>> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591
