using System.Collections.Generic;

namespace tpldfexplore.Dataflow
{
    public interface IAccumulator<T> : IList<T>
    {
        void AddRange(IEnumerable<T> collection);
    }
}