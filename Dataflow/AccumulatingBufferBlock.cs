using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace tpldfexplore.Dataflow
{

    public interface IAccumulatingBufferBlock<T> : IPropagatorBlock<IEnumerable<T>, IEnumerable<T>>
    {
    }

    public class AccumulatingBufferBlock<T> : IAccumulatingBufferBlock<T>
    {
        public IAccumulator<T> Accumulator { get; }
        public ILogger<AccumulatingBufferBlock<T>> Logger { get; }

        static object AccumulatorLock = new object();

        ActionBlock<IEnumerable<T>> AccumBlock;

        Task completion;
        public Task Completion
        {
            get { return completion; }
            set
            {
                if (completion == null)
                {
                    completion = value;
                }
                else
                {
                    completion.ContinueWith(t => value);
                }
            }
        }

        public AccumulatingBufferBlock(IAccumulator<T> accumulator, ILogger<AccumulatingBufferBlock<T>> logger)
        {
            Accumulator = accumulator;
            Logger = logger;

            AccumBlock = new ActionBlock<IEnumerable<T>>(itemsEnumerable =>
            {
                var items = itemsEnumerable.ToArray();
                lock (AccumulatorLock)
                {
                    Logger.LogDebug($"AccumBlock adding {items.Length} items.");
                    Accumulator.AddRange(items);
                    Logger.LogDebug("AccumBlock adding done.");
                }
            });
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, IEnumerable<T> messageValue,
            ISourceBlock<IEnumerable<T>> source, bool consumeToAccept)
        {
            return ((ITargetBlock<IEnumerable<T>>)AccumBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            ((ITargetBlock<IEnumerable<T>>)AccumBlock).Complete();
            Logger.LogTrace("COMPLETE.");
        }

        public void Fault(Exception exception)
        {
            ((ITargetBlock<IEnumerable<T>>)AccumBlock).Fault(exception);
            Logger.LogTrace("FAULT.");
        }

        public IDisposable LinkTo(ITargetBlock<IEnumerable<T>> target, DataflowLinkOptions linkOptions)
        {
            var emitter = new WriteOnceBlock<IEnumerable<T>>(items => items);
            // When the AccumBlock completes, send all the items to the emitter and complete it.
            AccumBlock.Completion.ContinueWith(async o =>
            {
                if (o.IsFaulted)
                {
                    ((ITargetBlock<IEnumerable<T>>)emitter).Fault(o.Exception);
                }
                else
                {
                    await emitter.SendAsync(Accumulator);
                    emitter.Complete();
                    Logger.LogTrace("emitter complete.");
                }
            });
            Completion = emitter.Completion;
            var disposable = emitter.LinkTo(target, linkOptions);
            return disposable;
        }

        public IEnumerable<T> ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<IEnumerable<T>> target, out bool messageConsumed)
        {
            throw new NotSupportedException("This is not supported. This is a custom Block implementation.");
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<IEnumerable<T>> target)
        {
            throw new NotSupportedException("This is not supported. This is a custom Block implementation.");
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<IEnumerable<T>> target)
        {
            throw new NotSupportedException("This is not supported. This is a custom Block implementation.");
        }
    }
}