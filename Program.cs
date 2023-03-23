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
                IPAddress ipToPing;
                string IPsCsv = String.Empty;

                if (IPAddress.TryParse(hostname, out ipToPing))
                {
                    IPsCsv = ipToPing.ToString();
                }
                else
                {
                    try
                    {
                        IPHostEntry entry = await Dns.GetHostEntryAsync(hostname);
                        IPsCsv = String.Join(" ", entry.AddressList.Select(i => i.ToString()));
                        if (opts.resolveOnly)
                        {
                            Console.WriteLine($"{hostname}\t{IPsCsv}");
                            return;
                        }
                        ipToPing = entry.AddressList[0];
                    }
                    catch (Exception ex)
                    {
                        if (!opts.printOnlyOnline)
                        {
                            Console.WriteLine($"{hostname}\t{ex.Message.Replace(' ', '_')}");
                        }
                        return;
                    }
                }

                PingReply reply = null;
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    reply = await ping.SendPingAsync(address: ipToPing);
                }
                if (!opts.printOnlyOnline)
                {
                    Console.WriteLine($"{hostname}\t{reply.Status.ToString()}\t{(reply.Status == IPStatus.Success ? IPsCsv : String.Empty)}");
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
