using CDTS_PROJECT.Logics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CDTS_PROJECT.Models;
using CDTS_PROJECT.Services;
using Microsoft.Extensions.Options;
using CDTS_PROJECT.Filters;
using Microsoft.AspNetCore.HttpOverrides;

namespace CDTS_PROJECT
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
            // requires using Microsoft.Extensions.Options
            services.Configure<ModelDatabaseSettings>(
                Configuration.GetSection(nameof(ModelDatabaseSettings)));

            services.AddSingleton<IModelDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<ModelDatabaseSettings>>().Value);

            services.AddSingleton<IModelService, ModelService>();
            services.AddSingleton<IencryptedOperationsService, encryptedOperationsService>();
            services.AddSingleton<IContextManager, ContextManager>();

            services.AddControllers(options => options.Filters.Add(new HttpResponseExceptionFilter()));
            services.AddRazorPages();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); 
            }

            //app.UseHttpsRedirection(); //not needed, handled by nginx proxy server
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            //app.UseAuthorization(); 

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
