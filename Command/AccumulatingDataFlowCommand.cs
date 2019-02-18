using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using tpldfexplore.Batch;
using tpldfexplore.Dataflow;

namespace tpldfexplore.Command
{
    public class AccumulatingDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public AccumulatingDataFlowCommand(
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer,
            ICompletionWriter<TTarget> completionWriter = null,
            IAccumulator<TTarget> accumulator = null
        )
        {
            Reader = reader;
            Processor = processor;
            Writer = writer;
            CompletionWriter = completionWriter;
            Accumulator = accumulator;
        }

        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }
        public ICompletionWriter<TTarget> CompletionWriter { get; }
        public IAccumulator<TTarget> Accumulator { get; }

        #endregion

        public void Run()
        {
            var context = new object();
            Task completionTask;
            var pipeline = BuildPipeline(context, out completionTask);

            foreach (var item in Reader.Items(AppConfig.ReadChunkSize, context))
            {
                pipeline.SendAsync(item).Wait(); // Do this instead of Post(item)
            }

            try
            {
                pipeline.Complete();
                Program.Log("pipeline.Complete() returned");

                completionTask.Wait();
                Program.Log("completionTask.Wait() returned");
            }
            catch (Exception e)
            {
                Program.Log($"ERROR: {e.GetType().ToString()} - {e.Message}");
                pipeline.Complete();
            }
        }

        protected ITargetBlock<TSource> BuildPipeline(object context, out Task completionTask)
        {
            var readerBlock = new BufferBlock<TSource>(new DataflowBlockOptions { BoundedCapacity = AppConfig.ReadChunkSize });
            var processorBlock = new TransformBlock<TSource, TTarget>(async item =>
            {
                return await Processor().Process(item, context);
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = AppConfig.ReadChunkSize,
                MaxDegreeOfParallelism = AppConfig.MaxDegreeOfParallelism,
                SingleProducerConstrained = true
            });
            var batchblock = new BatchBlock<TTarget>(AppConfig.ReadChunkSize);
            var taskScheduler = new ConcurrentExclusiveSchedulerPair();
            var writerBlock = new ActionBlock<ICollection<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items, context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = AppConfig.MaxDegreeOfParallelism,
                TaskScheduler = taskScheduler.ExclusiveScheduler
            });

            readerBlock.LinkToWithPropagation(processorBlock);
            processorBlock.LinkToWithPropagation(batchblock);
            batchblock.LinkToWithPropagation(writerBlock);

            completionTask = new Task(() =>
            {
                writerBlock.Completion.Wait();
                Program.Log("writerBlock.Completion.Wait() returned");
                if (CompletionWriter != null && Accumulator != null)
                {
                    Program.Log("Calling CompletionWriter");
                    CompletionWriter.Write(Accumulator, context).Wait();
                    Program.Log("CompletionWriter returned");
                }
            });

            completionTask.Start();

            return readerBlock;
        }
    }

    public class AccumulatingWriter<TTarget> : IWriter<TTarget>
    {
        public IAccumulator<TTarget> Accumulator { get; }
        static object AccumulatorLock = new object();

        public AccumulatingWriter(IAccumulator<TTarget> accumulator)
        {
            Accumulator = accumulator;
        }

        public Task Write(ICollection<TTarget> items, object context)
        {
            return Task.Run(() =>
            {
                lock (AccumulatorLock)
                {
                    Program.Log($"AccumuatingWriter.Write adding {items.Count} items.");
                    Accumulator.AddRange(items);
                    Program.Log("AccumuatingWriter.Write done.");
                }
            });
        }
    }
}