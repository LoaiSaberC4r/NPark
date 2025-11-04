namespace NPark.Application.Shared.Dto
{
    public struct StartConfigPacket
    {
        public byte Hour { get; set; }
        public byte Minute { get; set; }
        public byte Day { get; set; }
        public byte Month { get; set; }
        public byte Year { get; set; }   // آخر رقمين فقط
        public byte GracePeriod { get; set; }
        public byte GateNo { get; set; }

        public static StartConfigPacket FromDateTime(DateTime dt, byte gracePeriod, byte gateNo)
        {
            return new StartConfigPacket
            {
                Hour = (byte)dt.Hour,
                Minute = (byte)dt.Minute,
                Day = (byte)dt.Day,
                Month = (byte)dt.Month,
                Year = (byte)(dt.Year % 100),
                GracePeriod = gracePeriod,
                GateNo = gateNo
            };
        }

        public byte[] ToBodyBytes()
        {
            return new[]
            {
                Hour,
                Minute,
                Day,
                Month,
                Year,
                GracePeriod,
                GateNo
            };
        }

        public byte[] ToFullPacket()
        {
            var body = ToBodyBytes();
            var full = new byte[body.Length + 2];
            full[0] = 0x7B; // Start byte
            Buffer.BlockCopy(body, 0, full, 1, body.Length);
            full[^1] = 0x7D; // End byte
            return full;
        }

        public static StartConfigPacket FromFullPacket(ReadOnlySpan<byte> packet)
        {
            if (packet.Length < 9 || packet[0] != 0x7B || packet[^1] != 0x7D)
                throw new ArgumentException("Invalid StartConfigPacket format");

            return new StartConfigPacket
            {
                Hour = packet[1],
                Minute = packet[2],
                Day = packet[3],
                Month = packet[4],
                Year = packet[5],
                GracePeriod = packet[6],
                GateNo = packet[7]
            };
        }

        public override string ToString()
        {
            return $"[{Day:D2}/{Month:D2}/20{Year:D2} {Hour:D2}:{Minute:D2}] Gate:{GateNo} Grace:{GracePeriod}";
        }
    }
}