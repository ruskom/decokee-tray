namespace DecokeeTray.Hardware
{
    /// <summary>
    /// Background HID listener for Quake knob and button input.
    /// </summary>
    internal sealed class QuakeInputWatcher : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Action<QuakeInput> _onInput;
        private readonly Lock _streamGate = new();
        private readonly Thread _thread;
        private bool _disposed;
        private DateTimeOffset _lastKnobEvent = DateTimeOffset.MinValue;
        private HidStream? _stream;

        /// <summary>
        /// Creates a quake input watcher instance.
        /// </summary>
        public QuakeInputWatcher(Action<QuakeInput> onInput)
        {
            _onInput = onInput;
            _thread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "Quake HID input listener"
            };
            _thread.Start();
        }

        /// <summary>
        /// Releases resources owned by this instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cancellation.Cancel();
            lock (_streamGate)
            {
                _stream?.Dispose();
                _stream = null;
            }

            if (!_thread.Join(TimeSpan.FromSeconds(1)))
                AppLog.Write("Quake HID input listener did not stop within the shutdown timeout.");

            _cancellation.Dispose();
        }

        /// <summary>
        /// Reads Quake HID input until cancellation.
        /// </summary>
        private void ReadLoop()
        {
            while (!_cancellation.IsCancellationRequested)
                try
                {
                    using var stream = Open();
                    lock (_streamGate) _stream = stream;

                    var buffer = new byte[33];
                    while (!_cancellation.IsCancellationRequested)
                    {
                        Array.Clear(buffer);
                        var read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0) continue;

                        var input = Decode(buffer.Take(read).ToArray());
                        if (input is not null) Emit(input);
                    }
                }
                catch (ObjectDisposedException) when (_cancellation.IsCancellationRequested)
                {
                    // Shutdown closes the stream to interrupt the blocking read.
                }
                catch (Exception ex)
                {
                    if (!_cancellation.IsCancellationRequested)
                    {
                        AppLog.Error("Quake HID input listener failed; retrying.", ex);
                        Thread.Sleep(1_000);
                    }
                }
                finally
                {
                    lock (_streamGate)
                    {
                        _stream = null;
                    }
                }
        }

        /// <summary>
        /// Emits a decoded input event.
        /// </summary>
        private void Emit(QuakeInput input)
        {
            if (input.Control == QuakeControl.Knob)
            {
                var now = DateTimeOffset.UtcNow;
                if (now - _lastKnobEvent < TimeSpan.FromMilliseconds(80)) return;

                _lastKnobEvent = now;
            }

            _onInput(input);
        }

        /// <summary>
        /// Decodes a HID report.
        /// </summary>
        private static QuakeInput? Decode(byte[] data)
        {
            if (data.Length == 0) return null;

            if (data[0] == 0x00 && data.Length > 1) data = data.Skip(1).ToArray();

            if (data.Length < 6 || data[0] != 0xA3 || data[1] != 0x03) return null;

            var payload = data.Skip(2).Take(3).ToArray();
            if (payload.Length == 3 && payload[0] == 0x03 && payload[1] == 0x01)
                return payload[2] switch
                {
                    0x01 => new QuakeInput(QuakeControl.Knob, KnobDirection.Right),
                    0x02 => new QuakeInput(QuakeControl.Knob, KnobDirection.Left),
                    _ => null
                };

            if (payload.Length == 3 && payload[0] == 0x03 && payload[1] == 0x02 && payload[2] == 0x01)
                return new QuakeInput(QuakeControl.Button, KnobDirection.Right);

            return null;
        }

        /// <summary>
        /// Opens a HID stream.
        /// </summary>
        private static HidStream Open()
        {
            return QuakeDevice.OpenInput();
        }
    }
}
