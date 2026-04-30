using NPark.Application.Abstraction;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildingBlock.Infrastracture.Service
{
    public sealed class MqttExitQrPacketDecoder : IMqttExitQrPacketDecoder
    {
        private const byte StartByte = 0x7B;
        private const byte EndByte = 0x7D;
        private const byte PacketConstantByte = 0xBD;

        public MqttExitQrPacketDecodeResult Decode(byte[] payloadBytes)
        {
            if (payloadBytes is null || payloadBytes.Length == 0)
            {
                return Fail("Mqtt.ExitQr.EmptyPayload", "MQTT payload is empty.");
            }

            var frameBytes = NormalizePayloadToBytes(payloadBytes);
            var rawFrameHex = Convert.ToHexString(frameBytes);

            // Expected:
            // 7B 00 05 BD 75 A7 14 A3 C3 7D
            if (frameBytes.Length < 6)
            {
                return Fail(
                    "Mqtt.ExitQr.FrameTooShort",
                    "MQTT exit QR frame is too short.",
                    rawFrameHex);
            }

            if (frameBytes[0] != StartByte)
            {
                return Fail(
                    "Mqtt.ExitQr.InvalidStartByte",
                    $"Invalid start byte. Expected 0x7B but got 0x{frameBytes[0]:X2}.",
                    rawFrameHex);
            }

            if (frameBytes[^1] != EndByte)
            {
                return Fail(
                    "Mqtt.ExitQr.InvalidEndByte",
                    $"Invalid end byte. Expected 0x7D but got 0x{frameBytes[^1]:X2}.",
                    rawFrameHex);
            }

            // Expected:
            // index 0 = 7B
            // index 1,2 = controller id
            // index 3 = BD
            if (frameBytes[3] != PacketConstantByte)
            {
                return Fail(
                    "Mqtt.ExitQr.InvalidConstantByte",
                    $"Invalid constant byte. Expected 0xBD but got 0x{frameBytes[3]:X2}.",
                    rawFrameHex);
            }

            var controllerIdBytes = frameBytes[1..3];

            var controllerId = (controllerIdBytes[0] << 8) | controllerIdBytes[1];
            var controllerIdHex = Convert.ToHexString(controllerIdBytes);

            // MyData = everything after BD and before 7D
            var myDataBytes = frameBytes[4..^1];

            if (myDataBytes.Length == 0)
            {
                return Fail(
                    "Mqtt.ExitQr.EmptyQrData",
                    "QR data part is empty.",
                    rawFrameHex);
            }

            var myDataHex = Convert.ToHexString(myDataBytes);

            // IMPORTANT:
            // Validation Text must be Base64 of MyData only.
            // Example:
            // MyDataHex = 75 A7 14 A3 C3
            // QrString  = dacUo8M=
            var qrString = Convert
                .ToBase64String(myDataBytes)
                .Trim()
                .Trim('"');

            return new MqttExitQrPacketDecodeResult
            {
                IsSuccess = true,
                RawFrameHex = rawFrameHex,
                ControllerId = controllerId,
                ControllerIdHex = controllerIdHex,
                MyDataHex = myDataHex,

                // هنخليها نفس MyDataHex عشان مفيش wrapping خلاص.
                ActualQrHex = myDataHex,

                // ده اللي هيتبعت للـ ValidateExitByTextCommand.Text
                QrString = qrString
            };
        }

        private static byte[] NormalizePayloadToBytes(byte[] payloadBytes)
        {
            // Case 1:
            // Device sends real bytes:
            // 7B 00 05 BD 75 A7 14 A3 C3 7D
            if (payloadBytes.Length > 0 && payloadBytes[0] == StartByte)
            {
                return payloadBytes;
            }

            // Case 2:
            // mosquitto_pub -m sends ASCII hex:
            // "7b0005bd75a714a3c37d"
            var asText = Encoding.ASCII.GetString(payloadBytes).Trim();

            var normalizedHex = asText
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (normalizedHex.Length > 0 &&
                normalizedHex.Length % 2 == 0 &&
                Regex.IsMatch(normalizedHex, "^[0-9a-fA-F]+$"))
            {
                return Convert.FromHexString(normalizedHex);
            }

            return payloadBytes;
        }

        private static MqttExitQrPacketDecodeResult Fail(
            string code,
            string message,
            string rawFrameHex = "")
        {
            return new MqttExitQrPacketDecodeResult
            {
                IsSuccess = false,
                ErrorCode = code,
                ErrorMessage = message,
                RawFrameHex = rawFrameHex
            };
        }
    }
}