using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Spi;

namespace pingp
{
    class Program
    {
        static int Main(string[] args)
        {
            Opts opts;
            if ( ! Opts.TryParse(args, out opts ) )
            {
                return 1;
            }
            try
            {
                TextReader hostnames;
                if (String.IsNullOrEmpty(opts.filename))
                {
                    hostnames = Console.In;
                }
                else
                {
                    hostnames = new StreamReader(opts.filename, detectEncodingFromByteOrderMarks: true);
                }
                using (hostnames)
                {
                    new MaxTasks().Start(
                        tasks: Spi.Misc.ReadLines(hostnames).Select(h => ResolveAndPingAsync(h, opts.resolveOnly)),
                        MaxParallel: 128)
                    .Wait();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 8;
            }
            return 0;
        }
        static async Task ResolveAndPingAsync(string hostname, bool resolveOnly)
        {
            try
            {
                IPHostEntry entry = await Dns.GetHostEntryAsync(hostname);

                if (resolveOnly)
                {
                    string ips = String.Join(" ", entry.AddressList.Select(i => i.ToString()));
                    Console.WriteLine($"{hostname}\t{ips}");
                    return;
                }

                IPAddress ipToPing = entry.AddressList[0];
                PingReply reply = null;
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    reply = await ping.SendPingAsync(address: ipToPing);
                }
                string IPs = null;
                if ( reply.Status == IPStatus.Success)
                {
                    IPs = "\t" + String.Join(" ", entry.AddressList.Select(i => i.ToString()));
                }
                Console.WriteLine($"{hostname}\t{reply.Status.ToString()}{IPs}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{hostname}\t{ex.Message.Replace(' ', '_')}");
            }
        }
    }
}
