using System.Collections.Generic;
using System.Threading.Tasks;

namespace tpldfexplore.Batch
{
    public interface IWriter<TTarget>
    {
        Task Write(ICollection<TTarget> items, object context);
    }
}