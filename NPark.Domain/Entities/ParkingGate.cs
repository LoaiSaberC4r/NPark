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

        public string? QrPrinterName { get; private set; }
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
            string? qrPrinterName = null,
            string? receiptPrinterName = null)
        {
            if (parkingSystemConfigurationId <= 0)
                throw new ArgumentException(
                    "ParkingSystemConfigurationId must be greater than zero.",
                    nameof(parkingSystemConfigurationId));

            return new ParkingGate
            {
                GateNumber = gateNumber,
                GateType = gateType,
                ParkingSystemConfigurationId = parkingSystemConfigurationId,

                LprIp = lprIp,
                PcIp = pcIp,
                ControlIp = controlIp,

                ONVIFCameraSettingId = onvifCameraSettingId,

                QrPrinterName = qrPrinterName,
                ReceiptPrinterName = receiptPrinterName
            };
        }

        public void AssignToConfiguration(int parkingSystemConfigurationId)
        {
            if (parkingSystemConfigurationId <= 0)
                throw new ArgumentException(
                    "ParkingSystemConfigurationId must be greater than zero.",
                    nameof(parkingSystemConfigurationId));

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
            string? qrPrinterName,
            string? receiptPrinterName)
        {
            QrPrinterName = qrPrinterName;
            ReceiptPrinterName = receiptPrinterName;
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
            string? qrPrinterName = null,
            string? receiptPrinterName = null)
        {
            GateNumber = gateNumber;
            GateType = gateType;
            LprIp = lprIp;
            PcIp = pcIp;
            ControlIp = controlIp;
            ONVIFCameraSettingId = onvifCameraSettingId;
            IsAssigned = isAssigned;
            AssignedTo = assignedTo;
            QrPrinterName = qrPrinterName;
            ReceiptPrinterName = receiptPrinterName;
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
    }
}