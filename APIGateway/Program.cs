using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace APIGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // OpenTelemetry with comprehensive metrics
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: "APIGateway",
                        serviceVersion: "1.0.0",
                        serviceInstanceId: Environment.MachineName))
                .WithMetrics(metrics =>
                {
                    metrics
                        // ASP.NET Core metrics (requests, connections)
                        .AddAspNetCoreInstrumentation()
                        // HttpClient metrics
                        .AddHttpClientInstrumentation()
                        // .NET Runtime metrics (GC, Memory, ThreadPool)
                        .AddRuntimeInstrumentation()
                        // Prometheus exporter
                        .AddPrometheusExporter();
                });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Only enable Swagger in Development
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddSwaggerGen();
            }

            // Load YARP configuration
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Prometheus metrics endpoint at /metrics
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            // Only use HTTPS redirection in production with proper certificates
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.MapReverseProxy();

            app.Run();
        }
    }
}
