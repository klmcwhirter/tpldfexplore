using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using tpldfexplore.Service;

namespace tpldfexplore.Batch
{
    public class Int2WordsProcessor : IProcessor<int, string>
    {
        public Int2WordsProcessor(Int2WordsService int2WordsService, ILogger<Int2WordsProcessor> logger)
        {
            Int2WordsService = int2WordsService;
            Logger = logger;
        }

        public Int2WordsService Int2WordsService { get; }
        public ILogger<Int2WordsProcessor> Logger { get; }

        public Task<string> Process(int item, object context)
        {
            return Task.Run(() =>
            {
                Logger.LogTrace($"Int2WordsProcessor.Process - starting");

                var rc = $"{item:000} - {Int2WordsService.ToWords(item)}";

                Logger.LogTrace($"Int2WordsProcessor.Process - done");

                return rc;
            });
        }
    }
}