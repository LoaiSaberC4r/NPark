using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class ONVIFCameraSetting : Entity<Guid>
    {
        public string UrlRequest { get; private set; } = string.Empty;
        public string UserName { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        private ONVIFCameraSetting()
        { }

        public static ONVIFCameraSetting Create(string urlRequest, string userName, string password)
            => new ONVIFCameraSetting
            {
                UrlRequest = urlRequest,
                UserName = userName,
                Password = password
            };

        public void Update(string urlRequest, string userName, string password)
        {
            UrlRequest = urlRequest;
            UserName = userName;
            Password = password;
        }
    }
}