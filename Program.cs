using Spi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace pingp
{
    class Program
    {
        static int Main(string[] args)
        {
            Opts opts;
            if ( ! Opts.TryParse(args, out opts, out List<string> argHostnames ) )
            {
                return 1;
            }
            try
            {
                new MaxTasks().Start(
                    tasks: hostsToProcess(opts.filename, argHostnames)
                             .Select(h => ResolveAndPingAsync(h.Trim(), opts.resolveOnly)),
                    MaxParallel: 512)
                .Wait();
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
                IPAddress ipToPing;
                string IPsCsv;

                if (IPAddress.TryParse(hostname, out ipToPing))
                {
                    IPsCsv = ipToPing.ToString();
#if DEBUG
                    Console.WriteLine($"IP parsed successfully: {ipToPing} from name {hostname}");
#endif
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"DNS lookup for: [{hostname}]");
#endif
                    IPHostEntry entry = await Dns.GetHostEntryAsync(hostname);

                    IPsCsv = String.Join(" ", entry.AddressList.Select(i => i.ToString()));
                    if (resolveOnly)
                    {
                        Console.WriteLine($"{hostname}\t{IPsCsv}");
                        return;
                    }

                    ipToPing = entry.AddressList[0];
                }

                PingReply reply = null;
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    reply = await ping.SendPingAsync(address: ipToPing);
                }

                Console.WriteLine($"{hostname}\t{reply.Status.ToString()}\t{(reply.Status == IPStatus.Success ? IPsCsv : String.Empty)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{hostname}\t{ex.Message.Replace(' ', '_')}");
            }
        }
        static IEnumerable<string> hostsToProcess(string filename, List<string> argHostnames)
        {
            TextReader hostnameReader = null;
            if (String.IsNullOrEmpty(filename))
            {
                if (argHostnames.Count == 0)
                {
                    hostnameReader = Console.In;
                }
            }
            else
            {
                hostnameReader = new StreamReader(filename, detectEncodingFromByteOrderMarks: true);
            }

            foreach (var h in argHostnames)
            {
                yield return h;
            }

            if ( hostnameReader != null )
            {
                using (hostnameReader)
                {
                    foreach (var h in Misc.ReadLines(hostnameReader))
                    {
                        yield return h;
                    }
                }
            }
        }
    }
}
