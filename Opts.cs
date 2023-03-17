using Spi;
using System;
using System.Collections.Generic;

namespace pingp
{
    class Opts
    {
        public bool resolveOnly;
        public string filename;

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
                .Add('h', "help",    OPTTYPE.BOOL,  "print usage",                v => showHelp = true)
                .Add('r', "resolve", OPTTYPE.BOOL,  "only resolve hostnames",     v => tmpOpts.resolveOnly = true)
                .Add('f', "file",    OPTTYPE.VALUE, "file with hostnames or IPs", v => tmpOpts.filename = v)
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
