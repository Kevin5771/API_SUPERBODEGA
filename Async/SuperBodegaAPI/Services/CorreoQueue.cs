using System.Collections.Concurrent;

namespace SuperBodegaAPI.Services
{
    public class CorreoQueue
    {
        private readonly ConcurrentQueue<(string to, string subject, string html)> _cola = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void Enqueue(string to, string subject, string html)
        {
            _cola.Enqueue((to, subject, html));
            _signal.Release();
        }

        public async Task<(string to, string subject, string html)> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _cola.TryDequeue(out var item);
            return item;
        }
    }
}
