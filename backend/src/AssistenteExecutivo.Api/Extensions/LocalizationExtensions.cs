using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace AssistenteExecutivo.Api.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("pt-BR"),
                new CultureInfo("pt-PT"),
                new CultureInfo("en-US"),
                new CultureInfo("es-ES"),
                new CultureInfo("it-IT"),
                new CultureInfo("fr-FR")
            };

            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            // Accept-Language header will be used to determine the culture
            options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
        });

        return services;
    }

    public static IApplicationBuilder UseLocalizationConfiguration(this IApplicationBuilder app)
    {
        var localizationOptions = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        return app;
    }
}

