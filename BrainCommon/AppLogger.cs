using System;
using System.Collections.Concurrent;
using System.IO;
#if !DEBUG
using System.IO.Compression;
#endif

namespace BrainCommon
{
    public static class AppLogger
    {
        private static readonly StreamWriter LogFile;

        static AppLogger()
        {
            var utcNow = DateTime.UtcNow;
            var logFn = $"{utcNow.Year}.{utcNow.Month}.{utcNow.Day}.{utcNow.Ticks}.log";
            try
            {
#if DEBUG
                var baseStream = File.Create(logFn);
#else
            var baseStream = new GZipStream(File.Create(logFn), CompressionMode.Compress);
#endif
                LogFile = new StreamWriter(baseStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                LogFile = new StreamWriter(new MemoryStream());
            }
            Logqueue = new ConcurrentQueue<Tuple<string, string>>();
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private static void ProcessExit(object sender, EventArgs e)
        {
            LogFile?.Dispose();
        }

        private static readonly ConcurrentQueue<Tuple<string, string>> Logqueue;
        private static bool _isWriting;

        private static void StartWrite()
        {
            if (!_isWriting && Logqueue.TryDequeue(out var tuple))
            {
                _isWriting = true;
                LogFile.WriteAsync(tuple.Item1).ContinueWith(_ =>
                {
                    LogFile.WriteLineAsync(tuple.Item2).ContinueWith(b =>
                    {
                        _isWriting = false;
                        StartWrite();
                    });
                });
            }
        }

        public static void Error(string log)
        {
            var dateTime = DateTime.Now;
            Logqueue.Enqueue(Tuple.Create($"{dateTime},{dateTime.Ticks},Error,", log));
            StartWrite();
            Console.WriteLine(log);
        }

        public static void Warning(string log)
        {
            var dateTime = DateTime.Now;
            Logqueue.Enqueue(Tuple.Create($"{dateTime},{dateTime.Ticks},Warn,", log));
            StartWrite();
            Console.WriteLine(log);
        }

        public static void Info(string log)
        {
            var dateTime = DateTime.Now;
            Logqueue.Enqueue(Tuple.Create($"{dateTime},{dateTime.Ticks},Info,", log));
            StartWrite();
            Console.WriteLine(log);
        }

        public static void Debug(string log)
        {
            var dateTime = DateTime.Now;
            Logqueue.Enqueue(Tuple.Create($"{dateTime},{dateTime.Ticks},Debug,", log));
            StartWrite();
            Console.WriteLine(log);
        }

        public static void Debug(object log)
        {
            Debug(log.ToString());
        }
    }
}