using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using tpldfexplore.Batch;
using tpldfexplore.Dataflow;

namespace tpldfexplore.Command
{
    public class AccumulatingBlockDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public AccumulatingBlockDataFlowCommand(
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer,
            ICompletionWriter<TTarget> completionWriter = null,
            Func<IAccumulatingBufferBlock<TTarget>> accumulatingBlock = null,
            bool accumulateWrites = true
        )
        {
            Reader = reader;
            Processor = processor;
            Writer = writer;
            CompletionWriter = completionWriter;
            AccumulatingBlock = accumulatingBlock;
            AccumulateWrites = accumulateWrites;
        }

        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }
        public ICompletionWriter<TTarget> CompletionWriter { get; }
        public Func<IAccumulatingBufferBlock<TTarget>> AccumulatingBlock { get; }
        public bool AccumulateWrites { get; }

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
            var writerBlock = new ActionBlock<IEnumerable<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items.ToArray(), context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = AppConfig.MaxDegreeOfParallelism
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
                Program.Log("writerBlock.Completion.Wait() returned");
                if (CompletionWriter != null)
                {
                    Program.Log("Calling CompletionWriter");
                    CompletionWriter.Write(null, context).Wait();
                    Program.Log("CompletionWriter returned");
                }
            });

            completionTask.Start();

            return readerBlock;
        }
    }
}