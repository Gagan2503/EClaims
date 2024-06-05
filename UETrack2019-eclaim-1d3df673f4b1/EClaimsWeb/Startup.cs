using EClaimsEntities;
using EClaimsRepository.Contracts;
using EClaimsWeb.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using NToastNotify;
using Hangfire;
using Hangfire.Dashboard;
using EClaimsWeb.CustomExceptionMiddleware;
using EClaimsWeb.Helpers;
using DinkToPdf.Contracts;
using DinkToPdf;

namespace EClaimsWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureCors();

            //services.AddCors();
            services.ConfigureIISIntegration();
            services.ConfigureLoggerService();
            services.ConfigureSqlServerContext(Configuration);
            services.ConfigureHangFireContext(Configuration);

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "DinkToPdf", "libwkhtmltox.dll"));

            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            /*
            services.AddAuthentication()
                   .AddCookie(options =>
                   {
                       options.LoginPath = "/Account/login";
                       options.AccessDeniedPath = "/Account/Forbidden/";
                   });
            services.AddAuthentication()
            .AddJwtBearer(cfg =>
            {
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["JwtToken:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = Configuration["JwtToken:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtToken:SecretKey"])),

                };
            });
            */

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/login";
                options.AccessDeniedPath = "/Account/denied";
                //options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
                options.Events = new CookieAuthenticationEvents()
                {
                    OnSigningIn = async context =>
                    {
                        var scheme = context.Properties.Items.Where(k => k.Key == ".AuthScheme").FirstOrDefault();
                        var claim = new Claim(scheme.Key, scheme.Value);
                        var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                        var userService = context.HttpContext.RequestServices.GetRequiredService(typeof(IRepositoryWrapper)) as IRepositoryWrapper;
                        var nameIdentifier = claimsIdentity.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier)?.Value;
                        if(scheme.Value == "microsoft")
                        {
                            if (claimsIdentity.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Upn)?.Value != null)
                                nameIdentifier = claimsIdentity.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Upn)?.Value;
                            else
                                nameIdentifier = claimsIdentity.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Name)?.Value;
                        }
                        
                        if (userService != null && nameIdentifier != null)
                        {
                            var appUser = await userService.MstUser.GetUserByExternalProviderAsync(nameIdentifier);
                            if (appUser is null)
                            {
                                
                                /*
                                appUser = userService.MstUser.CreateExternalUser(scheme.Value, claimsIdentity.Claims.ToList());
                                await userService.SaveAsync();

                                var role = await userService.MstRole.GetRoleByIdAsync(4);

                                userService.DtUserRoles.CreateUserRoles(new EClaimsEntities.Models.DtUserRoles { RoleID = role.RoleID, UserID = appUser.UserID });
                                await userService.SaveAsync();

                                //userService.SaveAsync();
                                */
                            }
                            else
                            {
                                var userRole = userService.DtUserRoles.GetAllRolesByUserIdAsync(appUser.UserID);
                                var isSuperior = await userService.MstUserApprovers.CheckWhetherUserIsSuperiorAsync(appUser.UserID);
                                if (claimsIdentity.Claims.FirstOrDefault(c => c.Type == "issuperior") is null)
                                {
                                    if (isSuperior is not null)
                                        claimsIdentity.AddClaim(new Claim("issuperior", "True"));
                                }
                                if (claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role) is null)
                                {
                                    foreach (var r in userRole)
                                    {
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, r.RoleName));
                                    }
                                }

                                var userFacilities = await userService.DtUserFacilities.GetAllFacilitiesByUserIdAsync(Convert.ToInt32(appUser.UserID));

                                //claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, appUser.UserName));
                                //claimsIdentity.AddClaim(new Claim("username", appUser.UserName));
                                if(claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName) is null)
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.GivenName, appUser.Name));
                                if (claimsIdentity.Claims.FirstOrDefault(c => c.Type == "userid") is null)
                                    claimsIdentity.AddClaim(new Claim("userid", appUser.UserID.ToString()));
                                if (claimsIdentity.Claims.FirstOrDefault(c => c.Type == "facilityid") is null)
                                    claimsIdentity.AddClaim(new Claim("facilityid", userFacilities.FirstOrDefault().FacilityID.ToString()));
                                if (claimsIdentity.Claims.FirstOrDefault(c => c.Type == "ishod") is null)
                                    claimsIdentity.AddClaim(new Claim("ishod", appUser.IsHOD.ToString()));
                            }
                        }
                        claimsIdentity.AddClaim(claim);
                        await Task.CompletedTask;
                    }
                };
            })
            .AddOpenIdConnect("google", options =>
            {
                options.Authority = Configuration["GoogleOpenId:Authority"];
                options.ClientId = Configuration["GoogleOpenId:ClientId"];
                options.ClientSecret = Configuration["GoogleOpenId:ClientSecret"];
                //options.CallbackPath = Configuration["GoogleOpenId:CallbackPath"];
                options.CallbackPath = Configuration["GoogleOpenId:CallbackPath"];
                options.SignedOutCallbackPath = Configuration["GoogleOpenId:SignedOutCallbackPath"];
                options.SaveTokens = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // map name claim to ClaimTypes.Name since Google doesn't provide the name claim in the ISO way.
                    NameClaimType = "name",
                };
            }).AddOpenIdConnect("okta", options =>
            {
                options.Authority = Configuration["OktaOpenId:Authority"];
                options.ClientId = Configuration["OktaOpenId:ClientId"];
                options.ClientSecret = Configuration["OktaOpenId:ClientSecret"];
                //options.CallbackPath = Configuration["OktaOpenId:CallbackPath"];
                options.CallbackPath = Configuration["OktaOpenId:CallbackPath"];
                options.SignedOutCallbackPath = Configuration["OktaOpenId:SignedOutCallbackPath"];
                options.ResponseType = "code";
                options.SaveTokens = false;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = async (context) =>
                    {
                        var redirectUri = context.ProtocolMessage.RedirectUri;
                        await Task.CompletedTask;
                    }
                };
            }).AddOpenIdConnect("microsoft", options =>
            {
                options.Authority = Configuration["MicrosoftOpenId:Instance"] + Configuration["MicrosoftOpenId:TenantId"];
                options.ClientId = Configuration["MicrosoftOpenId:ClientId"];
                //options.ClientSecret = Configuration["MicrosoftOpenId:ClientSecret"];
                //options.CallbackPath = Configuration["MicrosoftOpenId:CallbackPath"];
                options.CallbackPath = Configuration["MicrosoftOpenId:CallbackPath"];
                options.SignedOutCallbackPath = Configuration["MicrosoftOpenId:SignedOutCallbackPath"];
                //options.ResponseType = "code";
                options.UseTokenLifetime = true;
                options.RequireHttpsMetadata = false;
                options.SaveTokens = false;
                //options.Scope.Add("openid");
                //options.Scope.Add("profile");
                //options.Events = new OpenIdConnectEvents()
                //{
                //    OnRedirectToIdentityProvider = async (context) =>
                //    {
                //        var redirectUri = context.ProtocolMessage.RedirectUri;
                //        await Task.CompletedTask;
                //    }
                //};
            });

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("ShouldBeOnlyHODPolicy", policy =>
                //     policy.RequireClaim("ishod", "True"));
                //options.AddPolicy("ShouldBeOnlySuperiorPolicy", policy =>
                //     policy.RequireClaim("issuperior", "True"));
                options.AddPolicy("ShouldBeOnlyHODORSuperiorPolicy", policy =>
                  policy.RequireAssertion(context => context.User.HasClaim(c => 
                                                                            (c.Type ==  "ishod" && c.Value.ToLower() =="true")||
                                                                             (c.Type == "issuperior" && c.Value.ToLower() == "true"))));
                // Policy to be applied to hangfire endpoint
                options.AddPolicy("HangfirePolicyName", builder =>
                {
                    builder
                        .RequireAuthenticatedUser();
                });
            });

            services.ConfigureRepositoryWrapper();
            services.AddAutoMapper(typeof(Startup));

            services.AddControllersWithViews()
                       .AddNewtonsoftJson(o => o.SerializerSettings.ReferenceLoopHandling =
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore);
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddTransient<RepositoryContext>();
            // Bootstrap Hangfire
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                );

            services.AddMvc().AddNewtonsoftJson(options =>
           options.SerializerSettings.ContractResolver =
              new CamelCasePropertyNamesContractResolver());

            //Notifications NToastNotify
            //Theme Options = relax, mint, metroui
            //top, topLeft, topCenter, topRight, center, centerLeft, centerRight, bottom, bottomLeft, bottomCenter, bottomRight
            services.AddMvc().AddNToastNotifyNoty(new NotyOptions(){Theme = "metroui",Layout = "topRight"});

            //services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            //{
            //    var libraryPath = Path.GetFullPath(
            //        Path.Combine(HostEnvironment.ContentRootPath, "..", "MyClassLib"));
            //    options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
            //if (env.IsDevelopment())
            //{
                app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //    app.UseHsts();
            //}
            app.UseNToastNotify();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.ConfigureCustomExceptionMiddleware();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            #region Configure hangfire dashoard and call send mail service function
            //SendMailServices _sendMailServices = new SendMailServices(env, configuration);
            SendMailServices _sendMailServices = new SendMailServices(env, Configuration,null,null);

            // Hangfire Settings
            app.UseHangfireServer();
            app.UseHangfireDashboard();
            RecurringJob.AddOrUpdate("Send Mail : Runs Every Tuesday Morning", () => _sendMailServices.SendPendingApprovalEmails("PendingApprovalMails.html"), Cron.Weekly(DayOfWeek.Monday, 17, 00));
            RecurringJob.AddOrUpdate("Send Mail : Runs Every Thursday Morning", () => _sendMailServices.SendPendingApprovalEmails("PendingApprovalMails.html"), Cron.Weekly(DayOfWeek.Wednesday, 17, 00));
            //RecurringJob.AddOrUpdate("Send Mail : Runs Every 5 Min", () => _sendMailServices.SendPendingApprovalEmails("PendingApprovalMails.html"),Cron.MinuteInterval(5));
            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHangfireDashboard("/hangfire", new DashboardOptions()
                {
                    Authorization = new List<IDashboardAuthorizationFilter> { }
                })
                    .RequireAuthorization("HangfirePolicyName");
            });
        }
    }
}
