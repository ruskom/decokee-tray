namespace DecokeeTray.Hardware
{
    /// <summary>
    /// High-level HID writer for Quake hardware controls.
    /// </summary>
    internal sealed class QuakeDevice
    {
        private const int VendorId = 0x4158;
        private const int ProductId = 0x514B;
        private const string DefaultPathContains = "mi_02";
        private static readonly Lock HidGate = new();

        /// <summary>
        /// Sends a Quake keepalive ping.
        /// </summary>
        public void Ping()
        {
            WriteShortCommand(0x02, 0xEF);
        }

        /// <summary>
        /// Updates the luminance slider.
        /// </summary>
        public void SetLuminance(int value)
        {
            WriteShortCommand(0x01, 0x05, ClampByte(value));
        }

        /// <summary>
        /// Sets the Quake knob LED state.
        /// </summary>
        public void SetKnobLed(bool enabled)
        {
            WriteShortCommand(0x01, 0x06, enabled ? (byte)1 : (byte)0);
        }

        /// <summary>
        /// Sets the QMK RGB matrix color and brightness.
        /// </summary>
        public void SetRgbMatrix(int brightness, int effect, Color color)
        {
            var (hue, saturation) = ToHsvBytes(color);
            WriteQmkCommand(0x07, 0x03, 0x02, ClampByte(effect));
            WriteQmkCommand(0x07, 0x03, 0x04, hue, saturation);
            WriteQmkCommand(0x07, 0x03, 0x01, ClampByte(brightness));
        }

        private static (byte Hue, byte Saturation) ToHsvBytes(Color color)
        {
            var hue = (byte)Math.Round(color.GetHue() / 360.0 * 255.0);
            var saturation = (byte)Math.Round(color.GetSaturation() * 255.0);
            return (hue, saturation);
        }

        /// <summary>
        /// Writes a compact Quake short command.
        /// </summary>
        private void WriteShortCommand(byte opCode, params byte[] data)
        {
            var payload = new byte[data.Length + 5];
            payload[0] = 0x00;
            payload[1] = 0xA3;
            payload[2] = (byte)(data.Length + 1);
            payload[3] = opCode;
            Buffer.BlockCopy(data, 0, payload, 4, data.Length);
            payload[^1] = Checksum(new[] { opCode }.Concat(data));
            Write(payload, false);
        }

        /// <summary>
        /// Writes a QMK command.
        /// </summary>
        private void WriteQmkCommand(params byte[] command)
        {
            Write(new[] { (byte)0x00 }.Concat(command).ToArray(), true);
        }

        /// <summary>
        /// Writes a log message.
        /// </summary>
        private void Write(byte[] payload, bool expectResponse)
        {
            lock (HidGate)
            {
                var device = FindDevice();
                var report = Pad(payload, device.GetMaxOutputReportLength());
                using var stream = Open(device);
                stream.Write(report, 0, report.Length);
                if (expectResponse) TryReadEcho(stream, report, payload.Length);
            }
        }

        /// <summary>
        /// Finds the Quake HID device.
        /// </summary>
        private static HidDevice FindDevice()
        {
            return DeviceList.Local
                       .GetHidDevices(VendorId, ProductId)
                       .Where(device => device.DevicePath.Contains(DefaultPathContains, StringComparison.OrdinalIgnoreCase))
                       .OrderByDescending(device => device.GetMaxOutputReportLength())
                       .FirstOrDefault()
                   ?? throw new InvalidOperationException("Quake HID device not found.");
        }

        /// <summary>
        /// Opens a HID stream.
        /// </summary>
        private static HidStream Open(HidDevice device)
        {
            if (!device.TryOpen(out var stream)) throw new InvalidOperationException("Failed to open Quake HID device.");

            stream.ReadTimeout = 500;
            stream.WriteTimeout = 5_000;
            return stream;
        }

        /// <summary>
        /// Opens the Quake input HID stream.
        /// </summary>
        public static HidStream OpenInput()
        {
            lock (HidGate)
            {
                var device = DeviceList.Local
                                 .GetHidDevices(VendorId, ProductId)
                                 .Where(device =>
                                     device.DevicePath.Contains(DefaultPathContains, StringComparison.OrdinalIgnoreCase))
                                 .OrderByDescending(device => device.GetMaxInputReportLength())
                                 .FirstOrDefault()
                             ?? throw new InvalidOperationException("Quake HID input device not found.");

                var stream = Open(device);
                stream.ReadTimeout = Timeout.Infinite;
                return stream;
            }
        }

        /// <summary>
        /// Attempts to read a command echo response.
        /// </summary>
        private static void TryReadEcho(HidStream stream, byte[] report, int commandLength)
        {
            var buffer = new byte[report.Length];
            try
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0) return;

                var response = buffer.Take(read).ToArray();
                if (!report.Take(commandLength).SequenceEqual(response.Take(commandLength)))
                {
                    // The hardware may emit unrelated input events. The setting still often applies.
                }
            }
            catch (TimeoutException)
            {
                // Some QMK writes apply without a response; do not kill the tray app.
            }
        }

        /// <summary>
        /// Pads a report payload to the required length.
        /// </summary>
        private static byte[] Pad(byte[] payload, int reportLength)
        {
            if (reportLength <= 0) return payload;

            if (payload.Length > reportLength)
                throw new InvalidOperationException("HID report is too long for the device.");

            var report = new byte[reportLength];
            Buffer.BlockCopy(payload, 0, report, 0, payload.Length);
            return report;
        }

        /// <summary>
        /// Clamps an integer to a byte value.
        /// </summary>
        private static byte ClampByte(int value)
        {
            return (byte)Math.Clamp(value, 0, 255);
        }

        /// <summary>
        /// Calculates the report checksum.
        /// </summary>
        private static byte Checksum(IEnumerable<byte> bytes)
        {
            var sum = 0;
            foreach (var value in bytes) sum += value;

            return (byte)(sum % 0xff);
        }
    }
}
