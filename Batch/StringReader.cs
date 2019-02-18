using System.Collections.Generic;
using System.Linq;

namespace tpldfexplore.Batch
{
    public class StringReader : IReader<int>
    {
        public StringReader(int count = 120)
        {
            Count = count;
        }

        public int Count { get; }

        public IEnumerable<int> Items(int chunkSize, object context)
        {
            Program.Log($"StringReader.Items - chunkSize={chunkSize}");

            var rc = Enumerable.Range(0, Count);

            Program.Log($"StringReader.Items - done.");
            return rc;
        }
    }
}