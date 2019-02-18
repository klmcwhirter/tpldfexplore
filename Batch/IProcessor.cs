using System.Collections.Generic;
using System.Threading.Tasks;

namespace tpldfexplore.Batch
{
    public interface IProcessor<TSource, TTarget>
    {
        Task<TTarget> Process(TSource item, object context);
    }
}