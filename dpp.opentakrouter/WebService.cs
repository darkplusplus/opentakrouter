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
                c.SwaggerDoc("internal", new OpenApiInfo { Title = "OpenTakRouter Internal API", Version = "v1" });
                c.SwaggerDoc("marti", new OpenApiInfo { Title = "OpenTakRouter MARTI Compatibility API", Version = "v1" });
                c.DocInclusionPredicate((documentName, apiDescription) =>
                {
                    var groupName = apiDescription.GroupName ?? "internal";
                    return string.Equals(documentName, groupName, System.StringComparison.OrdinalIgnoreCase);
                });
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
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/internal/swagger.json", "OpenTakRouter Internal API");
                        c.SwaggerEndpoint("/swagger/marti/swagger.json", "OpenTakRouter MARTI Compatibility API");
                    });
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
