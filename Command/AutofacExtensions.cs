using System;
using System.Collections.Generic;
using Autofac;
using tpldfexplore.Batch;
using tpldfexplore.Dataflow;

namespace tpldfexplore.Command
{
    public static class AutofacExtensions
    {
        public static void RegisterCommands(this ContainerBuilder builder, string name, int iterations = 100)
        {

            if (name == "simple")
            {
                builder.RegisterType<StringReader>().As<IReader<int>>().WithParameter(new NamedParameter("count", iterations));
                builder.RegisterType<Int2WordsProcessor>().As<IProcessor<int, string>>();
                builder.RegisterType<LoggerWriter<string>>().As<IWriter<string>>();
                builder.RegisterType<SimpleDataFlowCommand<int, string>>().As<ICommand>();
            }

            else if (name == "accumulating")
            {
                builder.RegisterType<StringReader>().As<IReader<int>>().WithParameter(new NamedParameter("count", iterations));
                builder.RegisterType<Int2WordsProcessor>().As<IProcessor<int, string>>();
                builder.RegisterType<AccumulatingWriter<string>>().As<IWriter<string>>();
                builder.RegisterType<LoggerWriter<string>>().As<ICompletionWriter<string>>();
                builder.RegisterType<ListAccumulator<string>>().As<IAccumulator<string>>().SingleInstance();
                builder.RegisterType<AccumulatingDataFlowCommand<int, string>>().As<ICommand>();
            }

            else if (name == "sa")
            {
                builder.RegisterType<StringReader>().As<IReader<int>>().WithParameter(new NamedParameter("count", iterations));
                builder.RegisterType<Int2WordsProcessor>().As<IProcessor<int, string>>();
                builder.RegisterType<LoggerWriter<string>>().As<IWriter<string>>();
                // builder.RegisterType<ConsoleWriter<string>>().As<ICompletionWriter<string>>();
                builder.RegisterType<AccumulatingDataFlowCommand<int, string>>().As<ICommand>();
            }

            else if (name == "accumblock")
            {
                builder.RegisterType<StringReader>().As<IReader<int>>().WithParameter(new NamedParameter("count", iterations));
                builder.RegisterType<Int2WordsProcessor>().As<IProcessor<int, string>>();
                builder.RegisterType<LoggerWriter<string>>().As<IWriter<string>>();
                builder.RegisterType<CompletedWriter<string>>().As<ICompletionWriter<string>>();
                builder.RegisterType<ListAccumulator<string>>().As<IAccumulator<string>>().SingleInstance();
                builder.RegisterType<AccumulatingBufferBlock<string>>().As<IAccumulatingBufferBlock<string>>();
                builder.RegisterType<AccumulatingBlockDataFlowCommand<int, string>>().As<ICommand>();
            }

            else if (name == "sab")
            {
                builder.RegisterType<StringReader>().As<IReader<int>>().WithParameter(new NamedParameter("count", iterations));
                builder.RegisterType<Int2WordsProcessor>().As<IProcessor<int, string>>();
                builder.RegisterType<LoggerWriter<string>>().As<IWriter<string>>();
                builder.RegisterType<CompletedWriter<string>>().As<ICompletionWriter<string>>();
                builder.RegisterType<AccumulatingBlockDataFlowCommand<int, string>>().As<ICommand>()
                    .WithParameter(new NamedParameter("accumulateWrites", false));
            }
        }


    }
}