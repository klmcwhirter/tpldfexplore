using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace tpldfexplore.Batch
{
    public class LoggerWriter<TTarget> : IWriter<TTarget>, ICompletionWriter<TTarget>
    {
        public LoggerWriter(
            IOptions<ProgramOptions> optionsAccessor,
            ILogger<LoggerWriter<TTarget>> logger)
        {
            Options = optionsAccessor?.Value;
            Logger = logger;
        }

        public ProgramOptions Options { get; }
        public ILogger<LoggerWriter<TTarget>> Logger { get; }

        public Task Write(ICollection<TTarget> items, object context)
        {
            return Task.Run(() =>
            {
                Logger.LogInformation($"LoggerWriter.Write: {items.Count} items to write");
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    foreach (var item in items)
                    {
                        Logger.LogTrace(item.ToString());
                    }
                }
                Logger.LogDebug($"LoggerWriter.Write done writing {items.Count} items.");
            });
        }
    }
}