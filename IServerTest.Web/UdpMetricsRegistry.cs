using App.Metrics;
using App.Metrics.Meter;
using App.Metrics.Timer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IServerTest.Web;

public static class UdpMetricsRegistry
{
    public static MeterOptions ClientRequestCount =>
        new()
        {
            Name = "ClientRequestCount",
            MeasurementUnit = Unit.Calls,
            RateUnit = TimeUnit.Seconds,
        };
    
    public static MeterOptions ServerRequestCount =>
        new()
        {
            Name = "ServerRequestCount",
            MeasurementUnit = Unit.Calls,
            RateUnit = TimeUnit.Seconds,
        };
    
    public static TimerOptions ClientE2ETime =>
        new()
        {
            Name = "ClientE2ETime",
            MeasurementUnit = Unit.Calls,
            RateUnit = TimeUnit.Milliseconds,
            DurationUnit = TimeUnit.Milliseconds,
        };
    
    public static TimerOptions ServerE2ETime =>
        new()
        {
            Name = "ServerE2ETime",
            MeasurementUnit = Unit.Calls,
            RateUnit = TimeUnit.Milliseconds,
            DurationUnit = TimeUnit.Milliseconds,
        };
}
