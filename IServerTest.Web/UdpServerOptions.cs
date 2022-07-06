using System.ComponentModel.DataAnnotations;
using System.Net;

namespace IServerTest.Web;

public class UdpServerOptions
{
    [Required]
    public IPAddress Address { get; set; } = IPAddress.Any;

    [Required]
    public int Port { get; set; }
}
