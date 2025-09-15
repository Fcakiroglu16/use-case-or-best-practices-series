namespace Todo.API.OpenTelemetry
{
    public class OpenTelemetryOption
    {
        public required string ServiceName { get; set; }

        public required string ServiceVersion { get; set; }

        public required string ActivitySourceName { get; set; }

        public required string OtelCollectorAddress { get; set; }

        public double? TraceIdRatioBasedSamplerRatio { get; set; }
    }
}