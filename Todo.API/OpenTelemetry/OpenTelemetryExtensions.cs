using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Todo.API.OpenTelemetry
{
    public static class OpenTelemetryExtensions
    {
        public static void AddOpenTelemetryExt(this IServiceCollection services, IConfiguration configuration)

        {
            services.Configure<OpenTelemetryConstants>(configuration.GetSection("OpenTelemetry"));
            var openTelemetryConstants = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConstants>()!;

            ActivitySourceProvider.Source =
                new System.Diagnostics.ActivitySource(openTelemetryConstants.ActivitySourceName);

            services.AddOpenTelemetry().WithTracing(options =>
            {
                options.AddSource(openTelemetryConstants.ActivitySourceName)
                    .ConfigureResource(resource =>
                    {
                        resource.AddService(openTelemetryConstants.ServiceName,
                            serviceVersion: openTelemetryConstants.ServiceVersion);
                    });
                options.AddAspNetCoreInstrumentation(aspnetcoreOptions =>
                {
                    aspnetcoreOptions.Filter = (context) =>
                        !string.IsNullOrEmpty(context.Request.Path.Value) &&
                        context.Request.Path.Value.Contains("api", StringComparison.InvariantCulture);
                });
                options.AddHttpClientInstrumentation();

                options.AddConsoleExporter();
                options.AddOtlpExporter();
            }).WithMetrics(configure =>
            {
                configure.AddAspNetCoreInstrumentation();
                configure.AddHttpClientInstrumentation();

                configure.AddRuntimeInstrumentation();
                configure.AddProcessInstrumentation();

                configure.AddOtlpExporter();
            }).WithLogging(configure =>
            {
                configure.AddConsoleExporter();
                configure.AddOtlpExporter();
            });
        }
    }
}