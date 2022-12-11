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

    public class SequentialLogParser : ILogParser 
    {
        private readonly FileInfo file;
        private readonly Func<string, string?> tryGetIdFromLine;

        public SequentialLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this.file = file;
            this.tryGetIdFromLine = tryGetIdFromLine;
        }
        
        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(file.FullName);
            return lines
                .Select(tryGetIdFromLine)
                .Where(id => id != null)
                .ToArray();
        }
    }

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
            var lines = File.ReadLines(file.FullName);
            var result = new List<string>();
            var works = new List<string>();
            var lockers = new List<object>();
            for (var i = 0; i < 10; i++)
            {
                works.Add(null);
                lockers.Add(new object());
                var thread = new Thread((threadNumber) =>
                {
                    while (true)
                    {
                        lock (lockers[(int)threadNumber])
                        {
                            works[(int)threadNumber] = null;
                            Monitor.Wait(lockers[(int)threadNumber]);
                        }
                        var result_line = tryGetIdFromLine(works[(int)threadNumber]);
                        if (result_line != null)
                        {
                            Monitor.Enter(result);
                            result.Add(result_line);
                            Monitor.Exit(result);
                        }
                    }
                });
                thread.Start(i);
            }
            foreach (var line in lines)
            {
                while (true)
                {
                    var wasRequest = false;
                    for (var i = 0; i < works.Count; i++)
                    {
                        if (works[i] != null)
                            continue;
                        wasRequest = true;
                        works[i] = line;
                        lock (lockers[i])
                        {
                            Monitor.Pulse(lockers[i]);
                        }
                        break;
                    }
                    if (wasRequest) break;
                }
            }
            
            return result.ToArray();
        }
    }

    public class ParallelLogParser : ILogParser
    {
        private readonly FileInfo file;
        private readonly Func<string, string?> tryGetIdFromLine;

        public ParallelLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this.file = file;
            this.tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(file.FullName);
            var result = new List<string>();
            Parallel.ForEach(lines, line => Adder.Add(tryGetIdFromLine, result, line));
            return result.ToArray();
        }
    }

    public class PLinqLogParser : ILogParser
    {
        private readonly FileInfo file;
        private readonly Func<string, string?> tryGetIdFromLine;

        public PLinqLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this.file = file;
            this.tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(file.FullName);
            return lines
                .AsParallel()
                .Select(tryGetIdFromLine)
                .Where(id => id != null)
                .ToArray();
        }
    }
}