#pragma checksum "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "cd3dc4fecdaa9f6fdeff92439e2005dfb4f6c35f32593c2152105f8654bac984"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Pages_Login), @"mvc.1.0.razor-page", @"/Pages/Login.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("RouteTemplate", "{handler?}")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"cd3dc4fecdaa9f6fdeff92439e2005dfb4f6c35f32593c2152105f8654bac984", @"/Pages/Login.cshtml")]
    #nullable restore
    public class Pages_Login : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    #nullable disable
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 5 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
  
    ViewBag.Title = "Login";

#line default
#line hidden
#nullable disable
            WriteLiteral("\n<h2>");
#nullable restore
#line 9 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
Write(ViewBag.Title);

#line default
#line hidden
#nullable disable
            WriteLiteral(@".</h2>

<div class=""container-scroller"">
    <div class=""container-fluid page-body-wrapper full-page-wrapper"">
        <div class=""content-wrapper d-flex align-items-stretch auth auth-img-bg"">
            <div class=""row flex-grow"">
                <div class=""col-lg-7 login-half-bg d-flex flex-row"">
                    <p class=""text-muted font-weight-medium text-center flex-grow align-self-end text-sm-left"">
                        Copyright &copy; 2021 <a href=""https://www.bootstrapdash.com/"" target=""_blank"" class=""text-muted"">UEMS SOLUTIONS</a>
                        All rights reserved.
                    </p>
                </div>
                <div class=""col-lg-5 d-flex align-items-center justify-content-center"">
                    <div class=""auth-form-transparent text-left p-3"">
                        <div class=""brand-logo"">
                            <img src=""https://www.uemsgroup.com/sg/images/logo1.png"" alt=""logo"">
                        </div>
                        <h4>Welcome back!");
            WriteLiteral("</h4>\n                        <h6 class=\"font-weight-light\">Happy to see you again!</h6>\n                        <form class=\"pt-3\">\n                            ");
#nullable restore
#line 29 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                       Write(Html.AntiForgeryToken());

#line default
#line hidden
#nullable disable
            WriteLiteral("\n                            <div class=\"form-group\">\n                                ");
#nullable restore
#line 31 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                           Write(Html.LabelFor(m => m.UserName, new { @class = "exampleInputEmail" }));

#line default
#line hidden
#nullable disable
            WriteLiteral(@"
                                <div class=""input-group"">
                                    <div class=""input-group-prepend bg-transparent"">
                                        <span class=""input-group-text bg-transparent border-right-0"">
                                            <i class=""mdi mdi-account-outline text-primary""></i>
                                        </span>
                                    </div>
");
            WriteLiteral("                                    ");
#nullable restore
#line 40 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                               Write(Html.TextBoxFor(m => m.UserName, new { @class = "form-control form-control-lg border-left-0" }));

#line default
#line hidden
#nullable disable
            WriteLiteral("\n                                    ");
#nullable restore
#line 41 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                               Write(Html.ValidationMessageFor(m => m.UserName, "", new { @class = "text-danger" }));

#line default
#line hidden
#nullable disable
            WriteLiteral("\n                                </div>\n                            </div>\n                            <div class=\"form-group\">\n");
            WriteLiteral("                                ");
#nullable restore
#line 46 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                           Write(Html.LabelFor(m => m.Password, new { @class = "col-md-2 control-label" }));

#line default
#line hidden
#nullable disable
            WriteLiteral(@"
                                <div class=""input-group"">
                                    <div class=""input-group-prepend bg-transparent"">
                                        <span class=""input-group-text bg-transparent border-right-0"">
                                            <i class=""mdi mdi-lock-outline text-primary""></i>
                                        </span>
                                    </div>
                                    ");
#nullable restore
#line 53 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                               Write(Html.PasswordFor(m => m.Password, new { @class = "form-control" }));

#line default
#line hidden
#nullable disable
            WriteLiteral("\n                                    ");
#nullable restore
#line 54 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Pages\Login.cshtml"
                               Write(Html.ValidationMessageFor(m => m.Password, "", new { @class = "text-danger" }));

#line default
#line hidden
#nullable disable
            WriteLiteral("\n");
            WriteLiteral(@"                                </div>
                            </div>
                            <div class=""my-2 d-flex justify-content-between align-items-center"">
                                <a href=""#"" class=""auth-link text-black"">Forgot password?</a>
                            </div>
                            <div class=""my-3"">
                                <a class=""btn btn-block btn-primary btn-lg font-weight-medium auth-form-btn""
                                   href=""../../pages/user/dashboard.html"">LOGIN</a>
                            </div>
                            <div class=""mb-2 d-flex"">
                                <a class=""btn btn-google auth-form-btn flex-grow""
                                   href=""../../pages/user/new-user-settings.html"">
                                    <i class=""mdi mdi-google mr-2""></i>Google
                                </a>
                            </div>
                        </form>
                    </div>
                </div");
            WriteLiteral(">\n            </div>\n        </div>\n        <!-- content-wrapper ends -->\n    </div>\n    <!-- page-body-wrapper ends -->\n</div>");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<EClaimsWeb.Models.LoginViewModel> Html { get; private set; } = default!;
        #nullable disable
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<EClaimsWeb.Models.LoginViewModel> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<EClaimsWeb.Models.LoginViewModel>)PageContext?.ViewData;
        public EClaimsWeb.Models.LoginViewModel Model => ViewData.Model;
    }
}
#pragma warning restore 1591
