namespace NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo
{
    public sealed record GetGateConnectionInfoQueryResponse
    {
        public Guid GateId { get; init; }
        public string? PcIp { get; init; }
        public string? LprIp { get; init; }
        public string? ControlIp { get; init; }

        public string? QrPrinterName { get; init; }
        public string? ReceiptPrinterName { get; init; }
        public bool HasQrPrinterName { get; init; }

        public bool HasReceiptPrinterName { get; init; }
        public bool? HasScanner { get; init; }

        public ONVIFCameraSettingInfoResponse? ONVIFCameraSetting { get; init; }
    }

    public sealed record ONVIFCameraSettingInfoResponse
    {
        public Guid Id { get; init; }

        // Mapped from ONVIFCameraSetting.UrlRequest only.
        public string Url { get; init; } = string.Empty;
    }
}