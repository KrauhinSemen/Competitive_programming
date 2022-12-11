using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogParsing.LogParsers
{
    public static class Adder
    {
        public static void Add(Func<string, string?> tryGetIdFromLine, List<string> storage, string line)
        {
            var result_line = tryGetIdFromLine(line);
            if (result_line != null)
            {
                Monitor.Enter(storage);
                storage.Add(result_line);
                Monitor.Exit(storage);
            }
        }
    }
}