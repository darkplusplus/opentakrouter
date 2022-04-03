using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;

namespace dpp.opentakrouter
{
    public class WebService
    {
        public IConfiguration Configuration { get; }
        public WebService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenTakRouter", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            var apiConfig = Configuration.GetSection("server:api").Get<WebConfig>();
            if (apiConfig is not null)
            {
                if (apiConfig.Swagger)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenTakRouter v1"));
                }

                if (apiConfig.Ssl)
                {
                    app.UseHttpsRedirection();
                }
            }

            app.UseStaticFiles();
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "server=web endpoint={RemoteIpAddress} method={RequestMethod} req={RequestPath} status={StatusCode} ms={Elapsed}";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
                };
            });
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
