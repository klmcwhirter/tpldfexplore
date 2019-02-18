using tpldfexplore.Batch;

namespace tpldfexplore.Command
{
    public interface IBatchCommand<TSource,TTarget,TReader,TProcessor,TWriter> : ICommand
    where TReader : IReader<TSource>
    where TProcessor : IProcessor<TSource, TTarget>
    where TWriter : IWriter<TTarget>
    {
    }
}