using System;
using System.Threading;
using System.Threading.Tasks;

namespace XP.ThreadedMutex
{
    class Program
    {        
        static async Task Main(string[] args)
        {
            using var mutex = new ThreadedMutex(nameof(SingleInstance));
            var acquired = await mutex.TryAcquireAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine("Acquired: {0}", acquired);
            
            if (acquired)
            {
                Console.ReadLine();
                mutex.Release();
                Console.WriteLine("Released");
            }
        }
    }

    /// <summary>
    /// Implements a named mutex that holds the lock in a dedicated thread.
    /// </summary>
    public sealed class ThreadedMutex : IDisposable
    {
        private readonly string _name;
        private readonly ManualResetEvent _manualResetEvent;

        public ThreadedMutex(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _manualResetEvent = new ManualResetEvent(false);
        }

        public Task<bool> TryAcquireAsync(TimeSpan timeout)
        {
            _manualResetEvent.Reset();

            var tcs = new TaskCompletionSource<bool>();

            var thread = new Thread(
                new ThreadStart(() =>
                {
                    using var mutex = new Mutex(false, _name);

                    if (!mutex.WaitOne(timeout))
                    {
                        tcs.SetResult(false);
                        return;
                    }

                    tcs.SetResult(true);
                    try
                    {
                        _manualResetEvent.WaitOne();
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }                                       
                }))
            {
                IsBackground = true
            };                
            thread.Start();
            return tcs.Task;            
        }

        public bool Release() => _manualResetEvent.Set();

        public void Dispose() => _manualResetEvent.Dispose();
    }
}
