using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tpldfexplore.Batch;

namespace tpldfexplore.Command
{
    public class SimpleDataFlowCommand<TSource, TTarget> : ICommand
    {
        #region Ctor / properties

        public SimpleDataFlowCommand(
            IOptions<ProgramOptions> optionsAccessor,
            IReader<TSource> reader,
            Func<IProcessor<TSource, TTarget>> processor,
            Func<IWriter<TTarget>> writer,
            ILogger<SimpleDataFlowCommand<TSource, TTarget>> logger
        )
        {
            Options = optionsAccessor?.Value;
            Reader = reader;
            Processor = processor;
            Writer = writer;
            Logger = logger;
        }

        public ProgramOptions Options { get; set; }
        public IReader<TSource> Reader { get; }
        public Func<IProcessor<TSource, TTarget>> Processor { get; }
        public Func<IWriter<TTarget>> Writer { get; }
        public ILogger<SimpleDataFlowCommand<TSource, TTarget>> Logger { get; }

        #endregion

        public void Run()
        {
            var context = new object();
            Task completionTask;
            var pipeline = BuildPipeline(context, out completionTask);

            foreach (var item in Reader.Items(Options.ReadChunkSize, context))
            {
                pipeline.SendAsync(item).Wait();
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
            var writerBlock = new ActionBlock<ICollection<TTarget>>(async items =>
            {
                var writer = Writer();
                await writer.Write(items, context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
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
                    Logger.LogDebug("writerBlock.Completion.Wait() returned");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "ERROR");
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