using Spi;
using System;
using System.Collections.Generic;

namespace pingp
{
    class Opts
    {
        public bool resolveOnly;
        public string filename;

        public static bool TryParse(string[] args, out Opts opts)
        {
            bool showHelp = false;
            opts = null;
            Opts tmpOpts = new Opts()
            {
                resolveOnly = false,
                filename = null
            };

            var options = new Spi.BeeOptsBuilder()
                .Add('h', "help", Spi.OPTTYPE.BOOL, "print usage", v => showHelp = true)
                .Add('r', "resolve", Spi.OPTTYPE.BOOL, "only resolve hostnames", v => tmpOpts.resolveOnly = true)
                .GetOpts();

            List<string> argValues = Spi.BeeOpts.Parse(args, options, OnUnknown: null);

            if (argValues.Count > 0 )
            {
                tmpOpts.filename = argValues[0];
            }

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
            Console.WriteLine("usage: pingp [OPTIONS] [filenameWithHostnames]\n");
            BeeOpts.PrintOptions(options);
        }
    }
}
