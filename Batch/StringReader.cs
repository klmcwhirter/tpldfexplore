using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace tpldfexplore.Batch
{
    public class StringReader : IReader<int>
    {
        public StringReader(ILogger<StringReader> logger, int count = 120)
        {
            Count = count;
            Logger = logger;
        }

        public int Count { get; }
        public ILogger<StringReader> Logger { get; }

        public IEnumerable<int> Items(int chunkSize, object context)
        {
            Logger.LogDebug($"StringReader.Items - chunkSize={chunkSize}");

            var rc = Enumerable.Range(0, Count);
            Task.Delay(20);

            Logger.LogTrace($"StringReader.Items - done.");
            return rc;
        }
    }
}