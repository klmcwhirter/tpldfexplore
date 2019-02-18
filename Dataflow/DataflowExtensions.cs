using System;
using System.Threading.Tasks.Dataflow;

namespace tpldfexplore.Dataflow
{
    public static class DataflowExtensions
    {
        public static IDisposable LinkToWithPropagation<T>(
            this ISourceBlock<T> source, ITargetBlock<T> target,
            DataflowLinkOptions options = null
            )
        {
            if (options == null)
            {
                options = new DataflowLinkOptions();
            }
            options.PropagateCompletion = true;
            return source.LinkTo(target, options);
        }
    }
}