using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.CameraManagement.Query
{
    internal sealed class GetCameraSnapshotQueryHandler
         : IQueryHandler<GetCameraSnapshotQuery, CameraSnapshotResult>
    {
        private readonly ICameraSnapshotService _cameraSnapshotService;
        private readonly IGenericRepository<ONVIFCameraSetting> _configurationRepository;

        public GetCameraSnapshotQueryHandler(
            ICameraSnapshotService cameraSnapshotService,
            IGenericRepository<ONVIFCameraSetting> configurationRepository)
        {
            _cameraSnapshotService = cameraSnapshotService;
            _configurationRepository = configurationRepository;
        }

        public async Task<Result<CameraSnapshotResult>> Handle(
            GetCameraSnapshotQuery request,
            CancellationToken cancellationToken)
        {
            var camera = await _configurationRepository.GetByPropertyAsync(
                x => x.UrlRequest == request.Url);

            if (camera == null)
            {
                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.NotFound", "Camera not found."));
            }

            return await _cameraSnapshotService.GetSnapshotAsync(
                camera.UrlRequest,
                camera.UserName,
                camera.Password,
                cancellationToken);
        }
    }
}