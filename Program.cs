using System;
using Autofac;

namespace tpldfexplore
{
    public static class Program
    {
        public static object consoleLock = new object();

        public static void Log(string msg)
        {
            if (!AppConfig.DoLog) return;

            lock (consoleLock)
            {
                Console.WriteLine(msg);
            }
        }

        static void Main(string[] args)
        {
            AutofacConfiguration.RegisterTypes(args[0]);
            var command = AutofacConfiguration.GetCommand();
            command.Run();
        }
    }

    public static class AppConfig
    {
        public const bool DoLog = true;

        public const int Iterations = 10000;

        public const int MaxDegreeOfParallelism = 8;

        public const int ReadChunkSize = 100;

    }
}
