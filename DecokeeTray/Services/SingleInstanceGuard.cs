namespace DecokeeTray.Services
{
    /// <summary>
    /// Holds the named mutex that prevents multiple tray app instances in the same user session.
    /// </summary>
    internal sealed class SingleInstanceGuard : IDisposable
    {
        private const string MutexName = @"Local\DecokeeQuakeTray";

        private readonly Mutex _mutex;
        private readonly bool _hasHandle;

        private SingleInstanceGuard(Mutex mutex, bool hasHandle)
        {
            _mutex = mutex;
            _hasHandle = hasHandle;
        }

        /// <summary>
        /// Gets whether this process owns the single-instance mutex.
        /// </summary>
        public bool HasHandle => _hasHandle;

        /// <summary>
        /// Attempts to acquire the tray app single-instance mutex.
        /// </summary>
        public static SingleInstanceGuard Acquire()
        {
            var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            if (createdNew)
            {
                return new SingleInstanceGuard(mutex, hasHandle: true);
            }

            try
            {
                if (mutex.WaitOne(TimeSpan.Zero))
                {
                    return new SingleInstanceGuard(mutex, hasHandle: true);
                }
            }
            catch (AbandonedMutexException)
            {
                return new SingleInstanceGuard(mutex, hasHandle: true);
            }

            return new SingleInstanceGuard(mutex, hasHandle: false);
        }

        /// <summary>
        /// Releases the mutex when this process acquired it.
        /// </summary>
        public void Dispose()
        {
            if (_hasHandle)
            {
                _mutex.ReleaseMutex();
            }

            _mutex.Dispose();
        }
    }
}
