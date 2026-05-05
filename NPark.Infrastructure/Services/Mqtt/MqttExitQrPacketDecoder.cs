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
                return Fail(
                    "Mqtt.ExitQr.EmptyPayload",
                    "MQTT payload is empty.");
            }

            // Case 1:
            // Full packet as real bytes:
            // 7B 00 05 BD 43 41 36 39 32 30 31 35 46 34 7D
            if (LooksLikeRealPacket(payloadBytes))
            {
                return DecodePacket(payloadBytes);
            }

            // Case 2:
            // Full packet as ASCII hex:
            // "7b0005bd434136393230313546347d"
            var payloadAsText = Encoding.ASCII.GetString(payloadBytes).Trim();

            if (LooksLikeHexText(payloadAsText))
            {
                var maybePacketBytes = TryConvertHexTextToBytes(payloadAsText);

                if (maybePacketBytes is not null && LooksLikeRealPacket(maybePacketBytes))
                {
                    return DecodePacket(maybePacketBytes);
                }
            }

            // Case 3:
            // RS485 ASCII card number:
            // 43 41 36 39 32 30 31 35 46 34 0D 0A
            // ASCII => CA692015F4\r\n
            var asciiQr = ExtractQrTextFromAsciiBytes(payloadBytes);

            if (IsExactFiveByteQrHex(asciiQr))
            {
                var rawFrameHex = Convert.ToHexString(payloadBytes);
                var myDataBytes = RemoveTrailingCrLf(payloadBytes);
                var myDataHex = Convert.ToHexString(myDataBytes);

                return Success(
                    rawFrameHex: rawFrameHex,
                    hasControllerId: false,
                    controllerId: null,
                    controllerIdHex: string.Empty,
                    myDataHex: myDataHex,
                    qrText: asciiQr);
            }

            // Case 4:
            // Direct text:
            // "CA692015F4"
            var directText = NormalizeQrText(payloadAsText);

            if (IsExactFiveByteQrHex(directText))
            {
                var rawFrameHex = Convert.ToHexString(payloadBytes);
                var myDataHex = Convert.ToHexString(Encoding.ASCII.GetBytes(directText));

                return Success(
                    rawFrameHex: rawFrameHex,
                    hasControllerId: false,
                    controllerId: null,
                    controllerIdHex: string.Empty,
                    myDataHex: myDataHex,
                    qrText: directText);
            }

            return Fail(
                "Mqtt.ExitQr.UnsupportedPayload",
                "MQTT payload is not a supported QR format. Expected full packet, ASCII hex QR, or direct 10-character QR hex text.",
                Convert.ToHexString(payloadBytes));
        }

        private static MqttExitQrPacketDecodeResult DecodePacket(byte[] frameBytes)
        {
            var rawFrameHex = Convert.ToHexString(frameBytes);

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

            if (frameBytes.Length <= 4 || frameBytes[3] != PacketConstantByte)
            {
                var actual = frameBytes.Length > 3
                    ? $"0x{frameBytes[3]:X2}"
                    : "missing";

                return Fail(
                    "Mqtt.ExitQr.InvalidConstantByte",
                    $"Invalid constant byte. Expected 0xBD but got {actual}.",
                    rawFrameHex);
            }

            var controllerIdBytes = frameBytes[1..3];

            var controllerId = (controllerIdBytes[0] << 8) | controllerIdBytes[1];
            var controllerIdHex = Convert.ToHexString(controllerIdBytes);

            // MyData = bytes after BD and before 7D
            // Example:
            // 43 41 36 39 32 30 31 35 46 34
            var myDataBytes = frameBytes[4..^1];

            if (myDataBytes.Length == 0)
            {
                return Fail(
                    "Mqtt.ExitQr.EmptyQrData",
                    "QR data part is empty.",
                    rawFrameHex);
            }

            var myDataHex = Convert.ToHexString(myDataBytes);

            // First expected packet mode:
            // MyData is ASCII for QR text.
            // 43 41 36 39 32 30 31 35 46 34 => CA692015F4
            var asciiQr = ExtractQrTextFromAsciiBytes(myDataBytes);

            if (IsExactFiveByteQrHex(asciiQr))
            {
                return Success(
                    rawFrameHex: rawFrameHex,
                    hasControllerId: true,
                    controllerId: controllerId,
                    controllerIdHex: controllerIdHex,
                    myDataHex: myDataHex,
                    qrText: asciiQr);
            }

            // Second supported packet mode:
            // MyData is the real 5 QR bytes.
            // CA 69 20 15 F4 => CA692015F4
            if (myDataBytes.Length == 5)
            {
                var qrText = Convert.ToHexString(myDataBytes);

                if (IsExactFiveByteQrHex(qrText))
                {
                    return Success(
                        rawFrameHex: rawFrameHex,
                        hasControllerId: true,
                        controllerId: controllerId,
                        controllerIdHex: controllerIdHex,
                        myDataHex: myDataHex,
                        qrText: qrText);
                }
            }

            return Fail(
                "Mqtt.ExitQr.UnsupportedQrData",
                "MQTT QR data is not valid ASCII QR hex text or real 5-byte QR data.",
                rawFrameHex);
        }

        private static MqttExitQrPacketDecodeResult Success(
            string rawFrameHex,
            bool hasControllerId,
            int? controllerId,
            string controllerIdHex,
            string myDataHex,
            string qrText)
        {
            var normalizedQrText = NormalizeQrText(qrText);

            return new MqttExitQrPacketDecodeResult
            {
                IsSuccess = true,
                RawFrameHex = rawFrameHex,
                HasControllerId = hasControllerId,
                ControllerId = controllerId,
                ControllerIdHex = controllerIdHex,
                MyDataHex = myDataHex,

                // Actual QR value after decoding ASCII.
                // Example: CA692015F4
                ActualQrHex = normalizedQrText,

                // This is what must go to ValidateExitByTextCommand.Text
                QrString = normalizedQrText
            };
        }

        private static bool LooksLikeRealPacket(byte[] payloadBytes)
        {
            return payloadBytes.Length >= 6 &&
                   payloadBytes[0] == StartByte &&
                   payloadBytes[^1] == EndByte;
        }

        private static byte[]? TryConvertHexTextToBytes(string text)
        {
            var normalized = NormalizeHexText(text);

            if (normalized.Length == 0)
                return null;

            if (normalized.Length % 2 != 0)
                return null;

            if (!Regex.IsMatch(normalized, "^[0-9a-fA-F]+$"))
                return null;

            try
            {
                return Convert.FromHexString(normalized);
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractQrTextFromAsciiBytes(byte[] bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;

            var cleanBytes = RemoveTrailingCrLf(bytes);

            var text = Encoding.ASCII
                .GetString(cleanBytes)
                .Trim();

            return NormalizeQrText(text);
        }

        private static byte[] RemoveTrailingCrLf(byte[] bytes)
        {
            var end = bytes.Length;

            while (end > 0 && (bytes[end - 1] == 0x0D || bytes[end - 1] == 0x0A))
            {
                end--;
            }

            if (end == bytes.Length)
                return bytes;

            return bytes[..end];
        }

        private static string NormalizeQrText(string value)
        {
            return value
                .Trim()
                .Trim('"')
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToUpperInvariant();
        }

        private static string NormalizeHexText(string value)
        {
            return value
                .Trim()
                .Trim('"')
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeHexText(string value)
        {
            var normalized = NormalizeHexText(value);

            return normalized.Length > 0 &&
                   normalized.Length % 2 == 0 &&
                   Regex.IsMatch(normalized, "^[0-9a-fA-F]+$");
        }

        private static bool IsExactFiveByteQrHex(string value)
        {
            var normalized = NormalizeQrText(value);

            return normalized.Length == 10 &&
                   Regex.IsMatch(normalized, "^[0-9A-F]{10}$");
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