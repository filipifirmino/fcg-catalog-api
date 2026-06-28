using System.Text.Json.Serialization;
using FCG_CATALOG_API.Api.Extensions;
using FCG_CATALOG_API.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Serilog;

namespace FCG_CATALOG_API.Api;

public class Startup
{
    public IConfiguration _Configuration { get;}

    public Startup(IConfiguration configuration)
    {
        _Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));

        services.AddControllers()
        .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddJwtAuthentication(_Configuration);
        services.AddAuthorizationPolicies();
        services.AddConfigureInfra(_Configuration);
        services.AddApplicationConfiguration();
        services.AddHttpClient();
        services.AddSwaggerGen(s =>
        {
            s.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG Catalog Games", Version = "v1" });
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            s.IncludeXmlComments(xmlPath);
            s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe o token JWT no formato: Bearer {token}"
            });
            s.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                }
            );
        });

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(_Configuration.GetConnectionString("Postgres"))
                .UseSnakeCaseNamingConvention());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG Catalog API v1");
            c.DisplayRequestDuration();
        });

        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"), branch =>
        {
            branch.UseHttpsRedirection();
        });

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<RequestTimingMiddleware>();
        app.UseGlobalExceptionHandler();
        app.UseEndpoints(endpoints =>
        {
           endpoints.MapControllers();
           endpoints.MapHealthChecks("/health", new HealthCheckOptions
           {
               ResponseWriter = async (context, report) =>
               {
                   context.Response.ContentType = "application/json";
                   var result = System.Text.Json.JsonSerializer.Serialize(new
                   {
                       status = report.Status.ToString(),
                       checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
                   });
                   await context.Response.WriteAsync(result);
               }
           }).AllowAnonymous();
        });

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
