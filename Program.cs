using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using SharpAdbClient;

namespace ScrCpyOnConnect
{
    class Program
    {
        static void Main(string[] args)
        {
            var monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));

            monitor.Start();

            var o = Observable
                .FromEventPattern<DeviceDataEventArgs>(m => monitor.DeviceConnected += m, m => monitor.DeviceConnected -= m)
                .Select(n => n?.EventArgs?.Device)
                .DistinctUntilChanged(n => n?.Serial)
                .Do(n =>
                {
                    if (n == null)
                        return;

                    try
                    {
                        if (Process.GetProcessesByName("scrcpy-noconsole")?.Any(b => b.StartInfo.Arguments.Contains(n.Serial)) == true)
                            return;
                    }
                    catch (Exception)
                    {
                    }
                    var u = new ProcessStartInfo("scrcpy-noconsole.exe", $@"-s {n.Serial}")
                    {
                        WorkingDirectory = Environment.CurrentDirectory
                    };

                    Console.WriteLine($"Starting scrcpy for: {n.Serial}:{n.Name}");
                    Process.Start(u);
                })
                .Subscribe();
            Console.ReadKey();

            monitor?.Dispose();
            o?.Dispose();
        }
    }
}
