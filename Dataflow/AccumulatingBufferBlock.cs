using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using tpldfexplore.Utils;

namespace tpldfexplore.Dataflow
{

    public interface IAccumulatingBufferBlock<T> : IPropagatorBlock<IEnumerable<T>, IEnumerable<T>>
    {
    }

    public class AccumulatingBufferBlock<T> : IAccumulatingBufferBlock<T>
    {
        public IAccumulator<T> Accumulator { get; }
        static object AccumulatorLock = new object();

        ActionBlock<IEnumerable<T>> AccumBlock;

        Task completion;
        public Task Completion
        {
            get { return completion; }
            set
            {
                if(completion == null)
                {
                    completion = value;
                }
                else
                {
                    completion.ContinueWith(t => value);
                }
            }
        }

        public AccumulatingBufferBlock(IAccumulator<T> accumulator)
        {
            Accumulator = accumulator;

            AccumBlock = new ActionBlock<IEnumerable<T>>(itemsEnumerable =>
            {
                var items = itemsEnumerable.ToArray();
                lock (AccumulatorLock)
                {
                    Program.Log($"AccumBlock adding {items.Length} items.");
                    Accumulator.AddRange(items);
                    Program.Log("AccumBlock adding done.");
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
        }

        public void Fault(Exception exception)
        {
            ((ITargetBlock<IEnumerable<T>>)AccumBlock).Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<IEnumerable<T>> target, DataflowLinkOptions linkOptions)
        {
            var emitter = new WriteOnceBlock<IEnumerable<T>>(items => items);
            // When the AccumBlock completes, send all the items to the emitter and complete it.
            AccumBlock.Completion.ContinueWith(o =>
            {
                emitter.SendAsync(Accumulator).Wait();
                emitter.Complete();
                Program.Log("emitter complete.");
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