using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tpldfexplore.Batch;
using tpldfexplore.Dataflow;

namespace tpldfexplore.Command
{
    public class AccumulatingBlockDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public AccumulatingBlockDataFlowCommand(
            IOptions<ProgramOptions> optionsAccessor,
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer,
            ILogger<AccumulatingBlockDataFlowCommand<TSource, TTarget>> logger,
            ICompletionWriter<TTarget> completionWriter = null,
            Func<IAccumulatingBufferBlock<TTarget>> accumulatingBlock = null,
            bool accumulateWrites = true
        )
        {
            Options = optionsAccessor?.Value;
            Reader = reader;
            Processor = processor;
            Writer = writer;
            Logger = logger;
            CompletionWriter = completionWriter;
            AccumulatingBlock = accumulatingBlock;
            AccumulateWrites = accumulateWrites;
        }

        public ProgramOptions Options { get; set; }
        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }
        public ILogger<AccumulatingBlockDataFlowCommand<TSource, TTarget>> Logger { get; }
        public ICompletionWriter<TTarget> CompletionWriter { get; }
        public Func<IAccumulatingBufferBlock<TTarget>> AccumulatingBlock { get; }
        public bool AccumulateWrites { get; }

        #endregion

        public void Run()
        {
            Logger.LogDebug("Run starting ...");
            var context = new object();
            Task completionTask;
            var pipeline = BuildPipeline(context, out completionTask);

            foreach (var item in Reader.Items(Options.ReadChunkSize, context))
            {
                pipeline.SendAsync(item).Wait(); // Do this instead of Post(item)
            }

            try
            {
                pipeline.Complete();
                Logger.LogTrace("pipeline.Complete() returned");

                completionTask.Wait();
                Logger.LogTrace("completionTask.Wait() returned");
            }
            catch (AggregateException e)
            {
                Logger.LogError(e.Flatten().InnerException, "ERROR");
                pipeline.Complete();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "ERROR");
                pipeline.Complete();
            }
            Logger.LogDebug("Run done.");
        }

        protected ITargetBlock<TSource> BuildPipeline(object context, out Task completionTask)
        {
            var readerBlock = new BufferBlock<TSource>(new DataflowBlockOptions { BoundedCapacity = Options.ReadChunkSize });
            var processorBlock = new TransformBlock<TSource, TTarget>(async item =>
            {
                return await Processor().Process(item, context);
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = Options.ReadChunkSize,
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
                SingleProducerConstrained = true
            });
            var batchblock = new BatchBlock<TTarget>(Options.ReadChunkSize);
            var writerBlock = new ActionBlock<IEnumerable<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items.ToArray(), context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism
            });

            readerBlock.LinkToWithPropagation(processorBlock);
            processorBlock.LinkToWithPropagation(batchblock);
            if (AccumulateWrites)
            {
                var accumBlock = AccumulatingBlock();
                batchblock.LinkToWithPropagation(accumBlock);
                accumBlock.LinkToWithPropagation(writerBlock);
            }
            else
            {
                batchblock.LinkToWithPropagation(writerBlock);
            }

            completionTask = new Task(() =>
            {
                writerBlock.Completion.Wait();
                Logger.LogTrace("writerBlock.Completion.Wait() returned");
                if (CompletionWriter != null)
                {
                    Logger.LogTrace("Calling CompletionWriter");
                    CompletionWriter.Write(null, context).Wait();
                    Logger.LogTrace("CompletionWriter returned");
                }
            });

            completionTask.Start();

            return readerBlock;
        }
    }
}