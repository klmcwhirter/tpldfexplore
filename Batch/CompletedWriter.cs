using System.Collections.Generic;
using System.Threading.Tasks;

namespace tpldfexplore.Batch
{
    public class CompletedWriter<T> : ICompletionWriter<T>
    {
        public Task Write(ICollection<T> items, object context)
        {
            return Task.Factory.StartNew(() => Program.Log("CompletedWriter<T>.Write: Completed."));
        }
    }
}