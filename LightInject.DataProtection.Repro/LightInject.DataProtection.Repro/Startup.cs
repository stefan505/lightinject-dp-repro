using System;
using System.Reflection;
using LightInject.DataProtection.Repro.Data;
using LightInject.DataProtection.Repro.Models;
using LightInject.DataProtection.Repro.Services;
using LightInject.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LightInject.DataProtection.Repro
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            services.AddDataProtection(options => options.ApplicationDiscriminator = "my-app")
                .SetApplicationName("app");

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();


            // ==================== Start Working scenario ====================

            var factory = new DefaultServiceProviderFactory();
            var sp = factory.CreateServiceProvider(services);
            var dp = sp.GetDataProtectionProvider();
            var property = dp.GetType().GetProperty("Purposes", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

            Console.WriteLine((property.GetValue(dp) as string[])[0]); // Should see "app" printed in console every time the app runs.

            // ==================== End Working scenario ====================


            // ==================== Start Breaking scenario ====================

            var container = new ServiceContainer();
            var li_sp = container.CreateServiceProvider(services);
            var li_dp = li_sp.GetDataProtectionProvider();
            var li_property = li_dp.GetType().GetProperty("Purposes", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

            Console.WriteLine((li_property.GetValue(li_dp) as string[])[0]); // Should see "my-app" printed in console every time the app runs.

            // ==================== End Breaking scenario ====================

            // Running this app a couple of times, results the in breaking scenario printing the path of the exe, instead of "my-app" and vica versa.

            Console.ReadLine();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
