﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalWebsite.Services;

namespace PersonalWebsite
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // HSTS
            services.AddHsts(o =>
            {
                o.Preload = true;
                o.IncludeSubDomains = true;
                o.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddSingleton<IBlogProvider, CachingBlogProvider>();

            services.AddRazorPages()
                    .AddRazorRuntimeCompilation();
            services.AddControllersWithViews()
                    .AddRazorRuntimeCompilation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // For nginx
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseStatusCodePages();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx => 
                {
                    ctx.CacheVersionedFiles();
                    ctx.ServeGzipFiles();
                }
            });
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseEndpoints(endpoints => 
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }

    public static class StaticFileExtentions
    {
        static FileExtensionContentTypeProvider _fileTypes = new FileExtensionContentTypeProvider();

        public static void CacheVersionedFiles(this StaticFileResponseContext ctx)
        {
            if (ctx.Context.Request.Query.ContainsKey("v"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
                ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(30).ToString("R", CultureInfo.InvariantCulture));
            }
        }

        // https://gunnarpeipman.com/aspnet-core-precompressed-files/
        public static void ServeGzipFiles(this StaticFileResponseContext context)
        {
            var headers = context.Context.Response.Headers;
            var contentType = headers["Content-Type"];

            if (contentType != "application/x-gzip" && !context.File.Name.EndsWith(".gz"))
            {
                return;
            }

            var fileNameToTry = context.File.Name.Substring(0, context.File.Name.Length - 3);

            if (_fileTypes.TryGetContentType(fileNameToTry, out var mimeType))
            {
                headers.Add("Content-Encoding", "gzip");
                headers["Content-Type"] = mimeType;
            }
        }
    }
}
