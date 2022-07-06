using IServerTest.Web;
using Microsoft.AspNetCore.Mvc;
using App.Metrics;
using App.Metrics.Scheduling;
using App.Metrics.Timer;

var builder = WebApplication.CreateBuilder(args);

builder
    .WebHost
    .UseUdpServer(opts => { opts.Port = 8085; });

builder.Services.AddSingleton<IMetrics>(services =>
{
    var metrics = new MetricsBuilder()
        .Report
        .ToConsole()
        .Build();

    var scheduler = new AppMetricsTaskScheduler(
        TimeSpan.FromSeconds(5),
        async () =>
        {
            await Task.WhenAll(metrics.ReportRunner.RunAllAsync()).ConfigureAwait(false);
        });

    scheduler.Start();

    return metrics;
});


var app = builder.Build();
app.MapGet("/", () => "Hello World");


app.Run();
