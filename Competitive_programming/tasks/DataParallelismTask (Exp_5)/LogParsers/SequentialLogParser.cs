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
                storage.Add(result_line);
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
            foreach (var line in lines)
            {
                var thread = new Thread(() => Adder.Add(tryGetIdFromLine, result, line));
                thread.Start();
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