using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogParsing.LogParsers
{
    public class ThreadLogParser : ILogParser
    {
        private readonly FileInfo file;
        private readonly Func<string, string?> tryGetIdFromLine;

        public ThreadLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this.file = file;
            this.tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(file.FullName).ToArray();
            var countLines = lines.Count();
            var result = new List<string>();
            var threads = new List<Thread>();
            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread((threadNumber) =>
                {
                    var step = (int)threadNumber;
                    while (true)
                    {
                        Adder.Add(tryGetIdFromLine, result, lines[step]);
                        step += 10;
                        if (step >= countLines)
                            break;
                    }
                });
                threads.Add(thread);
                thread.Start(i);
            }
            for (var i = 0; i < threads.Count(); i++)
                threads[i].Join();
            return result.ToArray();
        }
    }
}