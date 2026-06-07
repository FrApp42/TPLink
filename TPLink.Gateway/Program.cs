using FrApp42.TPLink;
using Microsoft.OpenApi.Models;
using TPLink.Gateway.Configuration;
using TPLink.Gateway.Filters;
using TPLink.Gateway.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Bind configuration sections.
builder.Services.Configure<RouterOptions>(builder.Configuration.GetSection(RouterOptions.SectionName));

RouterOptions routerOptions = builder.Configuration.GetSection(RouterOptions.SectionName).Get<RouterOptions>()
                              ?? new RouterOptions();
SwaggerOptions swaggerOptions = builder.Configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>()
                                ?? new SwaggerOptions();

// Single shared router client, like the reference bridge.
builder.Services.AddSingleton(_ => new Client(routerOptions.Url, routerOptions.Login, routerOptions.Password));

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());

builder.Services.AddEndpointsApiExplorer();

// Swagger generation is gated behind the "Swagger:Enabled" setting in appsettings.json.
if (swaggerOptions.Enabled)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(swaggerOptions.Version, new OpenApiInfo
        {
            Title = swaggerOptions.Title,
            Version = swaggerOptions.Version,
            Description = swaggerOptions.Description
        });

        OpenApiSecurityScheme basicScheme = new()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            In = ParameterLocation.Header,
            Description = "HTTP Basic authentication using the credentials from Authentication:Users."
        };
        options.AddSecurityDefinition("basic", basicScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" }
                },
                Array.Empty<string>()
            }
        });
    });
}

WebApplication app = builder.Build();

if (swaggerOptions.Enabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{swaggerOptions.Version}/swagger.json", swaggerOptions.Title);
        options.DocumentTitle = swaggerOptions.Title;
        options.RoutePrefix = swaggerOptions.RoutePrefix;
    });
}

app.UseMiddleware<BasicAuthMiddleware>();

app.MapControllers();

app.Run();
