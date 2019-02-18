using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using tpldfexplore.Batch;

namespace tpldfexplore.Command
{
    public class SimpleDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public SimpleDataFlowCommand(
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer
        )
        {
            Reader = reader;
            Processor = processor;
            Writer = writer;
        }

        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }

        #endregion

        public void Run()
        {
            var context = new object();
            Task completionTask;
            var pipeline = BuildPipeline(context, out completionTask);

            foreach (var item in Reader.Items(AppConfig.ReadChunkSize, context))
            {
                pipeline.SendAsync(item).Wait();
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
            var writerBlock = new ActionBlock<ICollection<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items, context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = AppConfig.MaxDegreeOfParallelism,
                SingleProducerConstrained = true
            });

            readerBlock.LinkTo(processorBlock, new DataflowLinkOptions { PropagateCompletion = true });
            processorBlock.LinkTo(batchblock, new DataflowLinkOptions { PropagateCompletion = true });
            batchblock.LinkTo(writerBlock, new DataflowLinkOptions { PropagateCompletion = true });

            completionTask = new Task(() =>
            {
                try
                {
                    writerBlock.Completion.Wait();
                    Program.Log("writerBlock.Completion.Wait() returned");
                }
                catch (Exception e)
                {
                    Program.Log($"ERROR: {e.GetType().ToString()} - {e.Message}");
                }
                finally
                {
                    readerBlock.Complete();
                }
            });

            completionTask.Start();

            return readerBlock;
        }
    }
}