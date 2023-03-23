using Spi;
using System;
using System.Collections.Generic;

namespace pingp
{
    class Opts
    {
        public bool resolveOnly;
        public bool printOnlyOnline = false;
        public string filename;
        public int parallelPings = 1024;
        public int timeout_ms = 200;
        public bool showRoundtrip = false;

        public static bool TryParse(string[] args, out Opts opts, out List<string> argHostnames)
        {
            bool showHelp = false;
            opts = null;
            Opts tmpOpts = new Opts()
            {
                resolveOnly = false,
                filename = null
            };

            var options = new Spi.BeeOptsBuilder()
                .Add('h', "help",      OPTTYPE.BOOL,  "print usage",                v => showHelp = true)
                .Add('r', "resolve",   OPTTYPE.BOOL,  "only resolve hostnames",     v => tmpOpts.resolveOnly = true)
                .Add('f', "file",      OPTTYPE.VALUE, "file with hostnames or IPs", v => tmpOpts.filename = v)
                .Add('o', "online",    OPTTYPE.BOOL,  "print only host were ping succeeds", v => tmpOpts.printOnlyOnline = true)
                .Add('p', "parallel",  OPTTYPE.VALUE, "max parallel pings",          v => tmpOpts.parallelPings = Convert.ToInt32(v) )
                .Add('t', "timeout",   OPTTYPE.VALUE, "timeout in milliseconds",     v => tmpOpts.timeout_ms = Convert.ToInt32(v))
                .Add('s', "roundtrip", OPTTYPE.BOOL,  "print roundtrip time (ms)",   v => tmpOpts.showRoundtrip = true)
                .GetOpts();

            argHostnames = Spi.BeeOpts.Parse(args, options, OnUnknown: null);

            if ( showHelp )
            {
                ShowUsage(options);
                return false;
            }

            opts = tmpOpts;
            return true;
        }
        static void ShowUsage(IEnumerable<BeeOpts> options)
        {
            Console.WriteLine("usage: pingp [OPTIONS] [hosts...]\n");
            BeeOpts.PrintOptions(options);
        }
    }
}
