using System.Collections.Generic;

namespace tpldfexplore.Batch
{
    public interface IReader<TSource>
    {
        IEnumerable<TSource> Items(int chunkSize, object context);
    }
}