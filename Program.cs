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
    struct ResolveResult
    {
        public IPAddress ip;
        public string    IPsResolved;
        public Exception DNSException;
    }
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
                             .Select(h => ResolveAndPingAsync(h.Trim(), opts)),
                    MaxParallel: 512)
                .Wait();
            }
            catch (FileNotFoundException fex)
            {
                Console.WriteLine($"file not found: {fex.FileName}");
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Hoppala");
                Console.Error.WriteLine(ex.ToString());
                return 8;
            }
            return 0;
        }
        static async Task ResolveAndPingAsync(string hostname, Opts opts)
        {
            try
            {
                var resolveResult = await TryResolveIP(hostname, opts);
                if ( resolveResult.ip == null )
                {
                    if (!opts.printOnlyOnline)
                    {
                        Console.WriteLine($"{hostname}\t{resolveResult.DNSException.Message}");
                    }
                    return;
                }

                if (opts.resolveOnly)
                {
                    Console.WriteLine($"{hostname}\t{resolveResult.IPsResolved}");
                    return;
                }


                PingReply reply = null;
                using (var ping = new Ping())
                {
                    reply = await ping.SendPingAsync(address: resolveResult.ip);
                }
                if (!opts.printOnlyOnline)
                {
                    Console.WriteLine($"{hostname}\t{reply.Status}\t{(reply.Status == IPStatus.Success ? resolveResult.IPsResolved : String.Empty)}");
                }
                else if ( reply.Status == IPStatus.Success )
                {
                    Console.WriteLine("{0}", hostname);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
        static async Task<ResolveResult> TryResolveIP(string hostname, Opts opts)
        {
            if (IPAddress.TryParse(hostname, out IPAddress ipToPing))
            {
                return new ResolveResult() { ip = ipToPing, IPsResolved = ipToPing.ToString() };
            }
            else
            {
                try
                {
                    IPHostEntry entry = await Dns.GetHostEntryAsync(hostname);
                    string IPs = String.Join(" ", entry.AddressList.Select(i => i.ToString()));
                    return new ResolveResult() { ip = entry.AddressList[0], IPsResolved = IPs };
                }
                catch (Exception ex)
                {
                    return new ResolveResult() { DNSException=ex };
                }
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
