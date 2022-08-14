// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;

using IdentityServerHost.Quickstart.UI;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using QianshiService.Auth.Data.Models;

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QianshiService.Auth
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var connection = Configuration.GetConnectionString("Default");
            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.EmitStaticAudienceClaim = true;
            })
                .AddConfigurationStore(option =>
                {
                    option.ConfigureDbContext = context =>
                    {
                        context.UseMySql(connection, ServerVersion.Parse("8.0.30"), sql =>
                        {
                            sql.MigrationsAssembly("QianshiService.Auth");
                        });
                    };
                })
                .AddOperationalStore(option =>
                {
                    option.ConfigureDbContext = context =>
                    {
                        context.UseMySql(connection, ServerVersion.Parse("8.3.00"), sql =>
                        {
                            sql.MigrationsAssembly("QianshiService.Auth");
                        });
                    };
                    option.EnableTokenCleanup = true;
                    option.TokenCleanupInterval = 30;
                });
            //.AddTestUsers(TestUsers.Users);

            services.AddDbContext<ApplicationDbContext>(option =>
            {
                option.UseMySql(connection, ServerVersion.Parse("8.0.30"));
            });

            // 更改Identity中关于用户和角色的处理到Entityframework
            services.AddIdentity<UserInfo, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.AddDeveloperSigningCredential();

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.ClientId = "copy client ID from Google here";
                    options.ClientSecret = "copy client secret from Google here";
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            InitalizeDatabase(app);

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }


        private void InitalizeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                IdentityRole role = null;
                if (!applicationDbContext.Roles.Any())
                {
                    role = new IdentityRole()
                    {
                        Name = "admin",
                        NormalizedName = "admin"
                    };
                    applicationDbContext.Roles.Add(role);
                }
                else
                {
                    role = applicationDbContext.Roles.Where(r => r.Name.Equals("Admin")).SingleOrDefault();
                }
                if (!applicationDbContext.Users.Any())
                {
                    var user = new UserInfo()
                    {
                        UserName = "administrator",
                        PasswordHash = "admin123456".Sha256(),
                        Email = "674268748@qq.com",
                        NormalizedUserName = "admin"
                    };
                    applicationDbContext.UserClaims.Add(new IdentityUserClaim<string>()
                    {
                        ClaimType = ClaimTypes.Country,
                        ClaimValue = "CSC",
                        UserId = user.Id
                    });
                    applicationDbContext.Set<UserInfo>().Add(user);
                    if (role != null)
                    {
                        applicationDbContext.UserRoles.Add(new IdentityUserRole<string>()
                        {
                            RoleId = role.Id,
                            UserId = user.Id
                        });
                    }

                }
                applicationDbContext.SaveChanges();


                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var api in Config.ApiScopes)
                    {
                        context.ApiScopes.Add(api.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}