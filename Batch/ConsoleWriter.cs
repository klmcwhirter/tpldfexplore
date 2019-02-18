using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace tpldfexplore.Batch
{
    public class ConsoleWriter<TTarget> : IWriter<TTarget>, ICompletionWriter<TTarget>
    {
        public Task Write(ICollection<TTarget> items, object context)
        {
            return Task.Run(() =>
            {
                if (!AppConfig.DoLog)
                {
                    Console.WriteLine($"ConsoleWriter.Write: {items.Count} items to write");
                }
                else
                {
                    lock (Program.consoleLock)
                    {
                        Program.Log($"ConsoleWriter.Write: {items.Count} items to write");
                        foreach (var item in items)
                        {
                            Program.Log(item.ToString());
                        }
                        Program.Log("ConsoleWriter.Write done.");
                    }
                }
            });
        }
    }
}