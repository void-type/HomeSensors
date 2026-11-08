using VoidCore.AspNet.Security;

namespace HomeSensors.Web.Startup;

public static class SecurityStartupExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IHostEnvironment env, IConfiguration config)
    {
        var vueDevServerEnabled = env.IsDevelopment() && config.GetSection("VueDevServer").GetValue("Enabled", true);
        var vueDevServerHost = config.GetSection("VueDevServer").GetValue("Host", string.Empty);

        app.UseContentSecurityPolicy(options =>
        {
            ConfigureBaseDirectives(options);

            if (vueDevServerEnabled)
            {
                ConfigureDevServerDirectives(options, vueDevServerHost);
            }
            else
            {
                options.ScriptSources
                    // Add the Swagger UI scripts
                    .AllowNonce();

                options.StyleSources
                    // Add the Swagger UI scripts
                    .AllowNonce();

                app.UseRecommendedSecurityHeaders();
            }
        });

        return app;
    }

    private static void ConfigureBaseDirectives(CspOptionsBuilder options)
    {
        options.BaseUri
            .AllowSelf();

        options.FrameSources
            .AllowNone();

        options.FrameAncestors
            .Allow("https://hass.void-type.net");

        options.DefaultSources
            .AllowSelf();

        options.ObjectSources
            .AllowNone();

        options.ImageSources
            .AllowSelf()
            // Bootstrap and other webpacked assets are loaded from inline data.
            .Allow("data:");

        options.ScriptSources
            .AllowSelf();

        options.StyleSources
            .AllowSelf();
    }

    private static void ConfigureDevServerDirectives(CspOptionsBuilder options, string vueDevServerHost)
    {
        // In development we need to allow unsafe eval of scripts for Vue's runtime compiler.
        options.ScriptSources
            // Vite dev server
            .Allow("https://" + vueDevServerHost)
            .AllowUnsafeInline()
            .AllowUnsafeEval();

        options.StyleSources
            // Vite dev server
            .Allow("https://" + vueDevServerHost)
            .AllowUnsafeInline();

        options.ImageSources
            .Allow("https://" + vueDevServerHost);

        // .NET and Vite hot reloading use web sockets
        options.ConnectSources
            .AllowSelf()
            // Vite dev server
            .Allow("https://" + vueDevServerHost)
            .Allow("ws:")
            .Allow("wss:");
    }
}
