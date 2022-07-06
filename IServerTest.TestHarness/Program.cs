// See https://aka.ms/new-console-template for more information

using App.Metrics;
using App.Metrics.Scheduling;
using App.Metrics.Timer;
using IServerTest;
using IServerTest.Web;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

var ts = new CancellationTokenSource();

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

var tasks = Enumerable.Range(0, 5)
    .Select(_ =>
        Task.Run(async () =>
        {
            var sw = new Stopwatch();

            while (!ts.IsCancellationRequested)
            {
                using var listener = new UdpClient();

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://google.com")
                {
                    Content = new StringContent($"{{ \"message\": \"Hello From Client!\" }}", Encoding.UTF8, "application/json")
                };

                sw.Start();

                _ = await listener.SendAsync(await new HttpRequestMessageTransformer().EncodeHttpRequest(httpRequest).ConfigureAwait(false), 
                    new IPEndPoint(IPAddress.Loopback, 8085)).ConfigureAwait(false);

                sw.Stop();

                metrics.Measure.Meter.Mark(UdpMetricsRegistry.ClientRequestCount);
                metrics.Measure.Timer.Time(UdpMetricsRegistry.ClientE2ETime, sw.ElapsedMilliseconds);
                sw.Reset();
            }
        }, ts.Token));

var allTasks = tasks.ToList();

Console.WriteLine("Press any key to cancel...");
Console.ReadKey();
ts.Cancel();

await Task.WhenAll(allTasks).ConfigureAwait(false);
