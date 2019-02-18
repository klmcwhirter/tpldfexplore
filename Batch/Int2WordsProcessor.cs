using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tpldfexplore.Service;

namespace tpldfexplore.Batch
{
    public class Int2WordsProcessor : IProcessor<int, string>
    {
        public Int2WordsProcessor(Int2WordsService int2WordsService)
        {
            Int2WordsService = int2WordsService;
        }

        public Int2WordsService Int2WordsService { get; }

        public Task<string> Process(int item, object context)
        {
            return Task.Run(() =>
            {
                Program.Log($"Int2WordsProcessor.Process - starting");

                var rc = $"{item:000} - {Int2WordsService.ToWords(item)}";

                Program.Log($"Int2WordsProcessor.Process - done");

                return rc;
            });
        }
    }
}