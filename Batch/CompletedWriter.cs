using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace tpldfexplore.Batch
{
    public class CompletedWriter<T> : ICompletionWriter<T>
    {
        public CompletedWriter(ILogger<CompletedWriter<T>> logger)
        {
            Logger = logger;
        }

        public ILogger<Batch.CompletedWriter<T>> Logger { get; }

        public Task Write(ICollection<T> items, object context)
        {
            return Task.Factory.StartNew(() => {
                Logger.LogInformation("CompletedWriter<T>.Write: Completed.");
                Task.Delay(20);
            });
        }
    }
}