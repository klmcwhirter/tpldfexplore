using System;

namespace tpldfexplore
{
    public class Program
    {
        public static ProgramOptions Options { get; set; }

        static void Main(string[] args)
        {
            int iterationsParsed;
            int? iterations = null;
            if (args.Length > 1 && int.TryParse(args[1], out iterationsParsed))
            {
                iterations = iterationsParsed;
            }

            int readChunkSizeParsed;
            int? readChunkSize = null;
            if (args.Length > 2 && int.TryParse(args[2], out readChunkSizeParsed))
            {
                readChunkSize = readChunkSizeParsed;
            }

            using (var startup = new Startup())
            {
                startup.Configure(args[0], iterations, readChunkSize);

                Options = startup.GetOptions();

                var command = startup.GetCommand();
                command.Run();
            }
        }
    }

    public class ProgramOptions
    {
        public bool DoLog { get; set; }

        public int MaxDegreeOfParallelism { get; set; }
        public int ReadChunkSize { get; set; }
    }
}
