using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tpldfexplore.Batch;
using tpldfexplore.Dataflow;

namespace tpldfexplore.Command
{
    public class AccumulatingDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public AccumulatingDataFlowCommand(
            IOptions<ProgramOptions> optionsAccssor,
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer,
            ILogger<AccumulatingDataFlowCommand<TSource, TTarget>> logger,
            ICompletionWriter<TTarget> completionWriter = null,
            IAccumulator<TTarget> accumulator = null
        )
        {
            Options = optionsAccssor?.Value;
            Reader = reader;
            Processor = processor;
            Writer = writer;
            Logger = logger;
            CompletionWriter = completionWriter;
            Accumulator = accumulator;
        }

        public ProgramOptions Options { get; set; }
        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }
        public ILogger<AccumulatingDataFlowCommand<TSource, TTarget>> Logger { get; }
        public ICompletionWriter<TTarget> CompletionWriter { get; }
        public IAccumulator<TTarget> Accumulator { get; }

        #endregion

        public void Run()
        {
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
                Logger.LogDebug("pipeline.Complete() returned");

                completionTask.Wait();
                Logger.LogDebug("completionTask.Wait() returned");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "ERROR");
                pipeline.Complete();
            }
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
            var taskScheduler = new ConcurrentExclusiveSchedulerPair();
            var writerBlock = new ActionBlock<ICollection<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items, context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
                TaskScheduler = taskScheduler.ExclusiveScheduler
            });

            readerBlock.LinkToWithPropagation(processorBlock);
            processorBlock.LinkToWithPropagation(batchblock);
            batchblock.LinkToWithPropagation(writerBlock);

            completionTask = new Task(() =>
            {
                writerBlock.Completion.Wait();
                Logger.LogDebug("writerBlock.Completion.Wait() returned");
                if (CompletionWriter != null && Accumulator != null)
                {
                    Logger.LogDebug("Calling CompletionWriter");
                    CompletionWriter.Write(Accumulator, context).Wait();
                    Logger.LogDebug("CompletionWriter returned");
                }
            });

            completionTask.Start();

            return readerBlock;
        }
    }

    public class AccumulatingWriter<TTarget> : IWriter<TTarget>
    {
        public IAccumulator<TTarget> Accumulator { get; }
        public ILogger<TTarget> Logger { get; }

        static object AccumulatorLock = new object();

        public AccumulatingWriter(IAccumulator<TTarget> accumulator, ILogger<TTarget> logger)
        {
            Accumulator = accumulator;
            Logger = logger;
        }

        public Task Write(ICollection<TTarget> items, object context)
        {
            return Task.Run(() =>
            {
                lock (AccumulatorLock)
                {
                    Logger.LogDebug($"AccumuatingWriter.Write adding {items.Count} items.");
                    Accumulator.AddRange(items);
                    Logger.LogDebug("AccumuatingWriter.Write done.");
                }
            });
        }
    }
}