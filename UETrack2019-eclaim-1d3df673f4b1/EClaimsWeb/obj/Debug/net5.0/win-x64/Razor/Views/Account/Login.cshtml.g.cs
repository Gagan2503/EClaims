#pragma checksum "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "690c9016fc5ec34921cbdade498018b43b1deb7e13b566948ba8a93a62afc457"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Account_Login), @"mvc.1.0.view", @"/Views/Account/Login.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"690c9016fc5ec34921cbdade498018b43b1deb7e13b566948ba8a93a62afc457", @"/Views/Account/Login.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA256", @"a4db12db2d232aa74afb152d1d99af05b5c5c936f2e6c6c148681d63b353a31b", @"/Views/_ViewImports.cshtml")]
    #nullable restore
    public class Views_Account_Login : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<EClaimsWeb.Models.LoginViewModel>
    #nullable disable
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("src", new global::Microsoft.AspNetCore.Html.HtmlString("~/images/logo.png"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("alt", new global::Microsoft.AspNetCore.Html.HtmlString("logo"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("asp-controller", "Account", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("asp-action", "Validate", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("btn btn-block btn-primary btn-lg font-weight-medium auth-form-btn"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("method", "post", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("role", new global::Microsoft.AspNetCore.Html.HtmlString("form"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_7 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("pt-3"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormActionTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormActionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 5 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
  
    //Layout = null;
    string returnUrl = ViewData["ReturnUrl"] as string;
    string loginFailedValue = ViewData["LoginFailed"] as string;
    bool loginFailed = false;
    if (!string.IsNullOrEmpty(loginFailedValue))
    {
        loginFailed = loginFailedValue == "True";
    }

    string username = ViewData["Username"] as string;
    var error = TempData["Error"] as string;
    Layout = "~/Views/Shared/_login.cshtml";

#line default
#line hidden
#nullable disable
            WriteLiteral(@"<div class=""container-scroller"">
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
                            ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("img", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "690c9016fc5ec34921cbdade498018b43b1deb7e13b566948ba8a93a62afc4577952", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\n                        </div>\n                        <h4>Welcome back!</h4>\n                        <h6 class=\"font-weight-light\">Happy to see you again!</h6>\n                        ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "690c9016fc5ec34921cbdade498018b43b1deb7e13b566948ba8a93a62afc4579278", async() => {
                WriteLiteral("\n");
#nullable restore
#line 38 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                             if (!string.IsNullOrEmpty(error))
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <span class=\"alert-danger\">");
#nullable restore
#line 40 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                                                      Write(error);

#line default
#line hidden
#nullable disable
                WriteLiteral("</span>\n");
#nullable restore
#line 41 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                            }

#line default
#line hidden
#nullable disable
#nullable restore
#line 42 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                             if (loginFailed)
                            {

#line default
#line hidden
#nullable disable
                WriteLiteral("                                <div class=\"login-failed\">Error: username or password incorrect</div>\n");
#nullable restore
#line 45 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                            }

#line default
#line hidden
#nullable disable
                WriteLiteral("                            ");
#nullable restore
#line 46 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                       Write(Html.AntiForgeryToken());

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                            <div class=\"form-group\">\n                                ");
#nullable restore
#line 48 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                           Write(Html.LabelFor(m => m.UserName));

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
#line 57 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                               Write(Html.TextBoxFor(m => m.UserName, new { @class = "form-control form-control-lg border-left-0", @placeholder = "Username" }));

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                                </div>\n                                ");
#nullable restore
#line 59 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                           Write(Html.ValidationMessageFor(m => m.UserName, "", new { @class = "text-danger" }));

#line default
#line hidden
#nullable disable
                WriteLiteral("\n                            </div>\n                            <div class=\"form-group\">\n");
                WriteLiteral("                                ");
#nullable restore
#line 63 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                           Write(Html.LabelFor(m => m.Password));

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
#line 70 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                               Write(Html.PasswordFor(m => m.Password, new { @class = "form-control form-control-lg border-left-0", @placeholder = "Password" }));

#line default
#line hidden
#nullable disable
                WriteLiteral("\n");
                WriteLiteral("                                </div>\n                                ");
#nullable restore
#line 74 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                           Write(Html.ValidationMessageFor(m => m.Password, "", new { @class = "text-danger" }));

#line default
#line hidden
#nullable disable
                WriteLiteral(@"
                            </div>
                            <div class=""my-2 d-flex justify-content-between align-items-center"">
                                <a href=""#"" class=""auth-link text-black"">Forgot password?</a>
                            </div>
                            <div class=""my-3"">
");
                WriteLiteral("                                ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "690c9016fc5ec34921cbdade498018b43b1deb7e13b566948ba8a93a62afc45715076", async() => {
                    WriteLiteral("LOGIN");
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_FormActionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormActionTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormActionTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_FormActionTagHelper.Controller = (string)__tagHelperAttribute_2.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_2);
                __Microsoft_AspNetCore_Mvc_TagHelpers_FormActionTagHelper.Action = (string)__tagHelperAttribute_3.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_3);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_4);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\n                                <input type=\"hidden\" name=\"returnUrl\"");
                BeginWriteAttribute("value", " value=\"", 5321, "\"", 5339, 1);
#nullable restore
#line 83 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
WriteAttributeValue("", 5329, returnUrl, 5329, 10, false);

#line default
#line hidden
#nullable disable
                EndWriteAttribute();
                WriteLiteral(" />\n                            </div>\n");
                WriteLiteral("                            <div class=\"mb-2 d-flex\">\n                                <a class=\"btn btn-google auth-form-btn flex-grow\"");
                BeginWriteAttribute("href", "\n                                   href=\"", 5879, "\"", 5958, 2);
                WriteAttributeValue("", 5921, "/login/microsoft?returnUrl=", 5921, 27, true);
#nullable restore
#line 93 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
WriteAttributeValue("", 5948, returnUrl, 5948, 10, false);

#line default
#line hidden
#nullable disable
                EndWriteAttribute();
                WriteLiteral(">\n                                    <i class=\"mdi mdi-office mr-2\"></i>Sign in with Microsoft\n                                </a>\n                            </div>\n                        ");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Method = (string)__tagHelperAttribute_5.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_5);
            BeginWriteTagHelperAttribute();
            WriteLiteral("/account/validate?returnUrl=");
#nullable restore
#line 36 "C:\Users\Dell\Desktop\UETrack2019-eclaim-1d3df673f4b1\EClaimsWeb\Views\Account\Login.cshtml"
                                                                        WriteLiteral(System.Net.WebUtility.UrlEncode(returnUrl));

#line default
#line hidden
#nullable disable
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Action = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("asp-action", __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Action, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_6);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_7);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\n                    </div>\n                </div>\n            </div>\n        </div>\n        <!-- content-wrapper ends -->\n    </div>\n    <!-- page-body-wrapper ends -->\n</div>\n");
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
    }
}
#pragma warning restore 1591
