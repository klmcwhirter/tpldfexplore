using System;

namespace tpldfexplore.Utils
{
    public class DisposableDisposer : IDisposable
    {
        public DisposableDisposer(params IDisposable[] disposables)
        {
            Disposables = disposables;
        }

        public IDisposable[] Disposables { get; }

        public void Dispose()
        {
            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }
        }
    }
}