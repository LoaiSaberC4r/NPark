using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo
{
    internal sealed class ParkingGateConnectionInfoReaderSpec :
        Specification<ParkingGate, GetGateConnectionInfoQueryResponse>
    {
        public ParkingGateConnectionInfoReaderSpec(Guid gateId)
        {
            AddCriteria(x => x.Id == gateId);
            Select(x => new GetGateConnectionInfoQueryResponse
            {
                ControlIp = x.ControlIp,
                LprIp = x.LprIp,
                PcIp = x.PcIp,
                GateId = x.Id,
                HasQrPrinterName = x.HasQrPrinterName,
                HasReceiptPrinterName = x.HasReceiptPrinterName,
                QrPrinterName = x.QrPrinterName,
                ReceiptPrinterName = x.ReceiptPrinterName,
                HasScanner = x.HasScanner,
                ONVIFCameraSetting = new ONVIFCameraSettingInfoResponse
                {
                    Url = x.ONVIFCameraSetting == null ? string.Empty : x.ONVIFCameraSetting.UrlRequest
                }
            });
        }
    }
}