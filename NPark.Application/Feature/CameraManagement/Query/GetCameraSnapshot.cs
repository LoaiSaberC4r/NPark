using BuildingBlock.Application.Abstraction;
using NPark.Application.Abstraction;

namespace NPark.Application.Feature.CameraManagement.Query
{
    public sealed record GetCameraSnapshotQuery : IQuery<CameraSnapshotResult>
    {
        public string Url { get; set; } = string.Empty;
    }
}