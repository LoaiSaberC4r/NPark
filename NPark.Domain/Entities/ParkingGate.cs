using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public class ParkingGate : Entity<Guid>
    {
        public int GateNumber { get; private set; }
        public GateType GateType { get; private set; }

        public bool? IsOccupied { get; private set; }
        public Guid? OccupiedBy { get; private set; }
        public DateTime? OccupiedTime { get; private set; }

        public string? PcIp { get; private set; } = string.Empty;
        public bool HasPc => !string.IsNullOrWhiteSpace(PcIp);

        public string? LprIp { get; private set; } = string.Empty;
        public bool HasLpr => !string.IsNullOrWhiteSpace(LprIp);

        public Guid? LastLoginUserId { get; private set; }

        public bool IsAssigned { get; private set; }
        public Guid? AssignedTo { get; private set; }
        public User? AssignedToUser { get; private set; }

        public string? ControlIp { get; private set; }

        public bool HasQrPrinterName { get; private set; }
        public string? QrPrinterName { get; private set; }

        public bool HasReceiptPrinterName { get; private set; }
        public string? ReceiptPrinterName { get; private set; }

        public Guid? ONVIFCameraSettingId { get; private set; }
        public ONVIFCameraSetting? ONVIFCameraSetting { get; private set; }

        public int ParkingSystemConfigurationId { get; private set; }
        public ParkingSystemConfiguration ParkingSystemConfiguration { get; private set; } = null!;

        private ParkingGate()
        {
        }

        public static ParkingGate Create(
            int gateNumber,
            GateType gateType,
            int parkingSystemConfigurationId,
            string? lprIp = null,
            string? pcIp = null,
            string? controlIp = null,
            Guid? onvifCameraSettingId = null,
            bool hasQrPrinterName = false,
            string? qrPrinterName = null,
            bool hasReceiptPrinterName = false,
            string? receiptPrinterName = null)
        {
            if (parkingSystemConfigurationId <= 0)
            {
                throw new ArgumentException(
                    "ParkingSystemConfigurationId must be greater than zero.",
                    nameof(parkingSystemConfigurationId));
            }

            var gate = new ParkingGate
            {
                GateNumber = gateNumber,
                GateType = gateType,
                ParkingSystemConfigurationId = parkingSystemConfigurationId,

                LprIp = lprIp,
                PcIp = pcIp,
                ControlIp = controlIp,

                ONVIFCameraSettingId = onvifCameraSettingId
            };

            gate.UpdatePrinters(
                hasQrPrinterName: hasQrPrinterName,
                qrPrinterName: qrPrinterName,
                hasReceiptPrinterName: hasReceiptPrinterName,
                receiptPrinterName: receiptPrinterName);

            return gate;
        }

        public void AssignToConfiguration(int parkingSystemConfigurationId)
        {
            if (parkingSystemConfigurationId <= 0)
            {
                throw new ArgumentException(
                    "ParkingSystemConfigurationId must be greater than zero.",
                    nameof(parkingSystemConfigurationId));
            }

            ParkingSystemConfigurationId = parkingSystemConfigurationId;
        }

        public void SetIsOccupied(
            bool isOccupied,
            Guid? occupiedBy = null,
            DateTime? occupiedTime = null)
        {
            IsOccupied = isOccupied;
            OccupiedBy = occupiedBy;
            OccupiedTime = occupiedTime;
        }

        public void UpdatePrinters(
            bool hasQrPrinterName,
            string? qrPrinterName,
            bool hasReceiptPrinterName,
            string? receiptPrinterName)
        {
            HasQrPrinterName = hasQrPrinterName;
            QrPrinterName = NormalizePrinterName(
                hasPrinterName: hasQrPrinterName,
                printerName: qrPrinterName,
                parameterName: nameof(qrPrinterName));

            HasReceiptPrinterName = hasReceiptPrinterName;
            ReceiptPrinterName = NormalizePrinterName(
                hasPrinterName: hasReceiptPrinterName,
                printerName: receiptPrinterName,
                parameterName: nameof(receiptPrinterName));
        }

        public void SetPcIp(string pcIp)
        {
            PcIp = pcIp;
        }

        public void UpdateGateInfo(
            int gateNumber,
            GateType gateType,
            string? lprIp,
            string? pcIp,
            string? controlIp,
            Guid? onvifCameraSettingId,
            bool isAssigned,
            Guid? assignedTo,
            bool hasQrPrinterName = false,
            string? qrPrinterName = null,
            bool hasReceiptPrinterName = false,
            string? receiptPrinterName = null)
        {
            GateNumber = gateNumber;
            GateType = gateType;
            LprIp = lprIp;
            PcIp = pcIp;
            ControlIp = controlIp;
            ONVIFCameraSettingId = onvifCameraSettingId;
            IsAssigned = isAssigned;
            AssignedTo = isAssigned ? assignedTo : null;

            UpdatePrinters(
                hasQrPrinterName: hasQrPrinterName,
                qrPrinterName: qrPrinterName,
                hasReceiptPrinterName: hasReceiptPrinterName,
                receiptPrinterName: receiptPrinterName);
        }

        public void SetLastLoginUserId(Guid userId)
        {
            LastLoginUserId = userId;
        }

        public void SetControlIp(string controlIp)
        {
            ControlIp = controlIp;
        }

        public void SetONVIFCameraSettingId(Guid? id)
        {
            ONVIFCameraSettingId = id;
        }

        private static string? NormalizePrinterName(
            bool hasPrinterName,
            string? printerName,
            string parameterName)
        {
            if (!hasPrinterName)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new ArgumentException(
                    $"{parameterName} is required when printer flag is enabled.",
                    parameterName);
            }

            return printerName.Trim();
        }
    }
}