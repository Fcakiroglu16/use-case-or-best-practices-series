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
            services.Configure<OpenTelemetryOption>(configuration.GetSection(nameof(OpenTelemetryOption)));
            var openTelemetryOption =
                configuration.GetSection(nameof(OpenTelemetryOption)).Get<OpenTelemetryOption>()!;

            ActivitySourceProvider.Source =
                new System.Diagnostics.ActivitySource(openTelemetryOption.ActivitySourceName);


            services.AddOpenTelemetry().WithTracing(options =>
            {
                options.AddSource(openTelemetryOption.ActivitySourceName)
                    .ConfigureResource(resource =>
                    {
                        resource.AddService(openTelemetryOption.ServiceName,
                            serviceVersion: openTelemetryOption.ServiceVersion);
                    });
                options.AddAspNetCoreInstrumentation(aspnetcoreOptions =>
                {
                    aspnetcoreOptions.Filter = (context) =>
                        !string.IsNullOrEmpty(context.Request.Path.Value) &&
                        context.Request.Path.Value.Contains("api", StringComparison.InvariantCulture);
                });
                options.AddHttpClientInstrumentation();

                options.AddConsoleExporter();
                options.AddOtlpExporter(x => x.Endpoint = new Uri(openTelemetryOption.OtelCollectorAddress));
            }).WithMetrics(configure =>
            {
                configure.ConfigureResource(resource =>
                {
                    resource.AddService(openTelemetryOption.ServiceName,
                        serviceVersion: openTelemetryOption.ServiceVersion);
                });


                configure.AddAspNetCoreInstrumentation();
                configure.AddProcessInstrumentation();
                configure.AddRuntimeInstrumentation();

                configure.AddOtlpExporter(x => x.Endpoint = new Uri(openTelemetryOption.OtelCollectorAddress));
            }).WithLogging(configure =>
            {
                configure.ConfigureResource(resource =>
                {
                    resource.AddService(openTelemetryOption.ServiceName,
                        serviceVersion: openTelemetryOption.ServiceVersion);
                });
                configure.AddConsoleExporter();
                configure.AddOtlpExporter(x => x.Endpoint = new Uri(openTelemetryOption.OtelCollectorAddress));
            });
        }
    }
}