using BuildingBlock.Domain.EntitiesHelper;

namespace CRM.Domain.Entities
{
    public sealed class Supporter : Entity<Guid>
    {
        private List<Ticket> assignedTickets = new List<Ticket>();
        private List<Ticket> closedTickets = new List<Ticket>();
        private List<Ticket> rejectedTickets = new List<Ticket>();
        public string Email { get; private set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public Guid RoleId { get; private set; }
        public Role Role { get; private set; } = null!;
        public bool IsFirstLogin { get; private set; } = true;
        public IReadOnlyCollection<Ticket> AssignedTickets => assignedTickets.AsReadOnly();
        public IReadOnlyCollection<Ticket> ClosedTickets => closedTickets.AsReadOnly();
        public IReadOnlyCollection<Ticket> RejectedTickets => rejectedTickets.AsReadOnly();

        private Supporter()
        { }

        public static Supporter Create(string email, string password, string phoneNumber, bool isFirstLogin = true)
        {
            return new Supporter()
            {
                Email = email,
                Password = password,
                PhoneNumber = phoneNumber,
                IsFirstLogin = isFirstLogin
            };
        }

        public void UpdateEmail(string email) => Email = email;

        public void UpdatePassword(string password) => Password = password;

        public void UpdatePhoneNumber(string phoneNumber) => PhoneNumber = phoneNumber;

        public void UpdateRole(Guid roleId) => RoleId = roleId;

        public void UpdateIsFirstLogin(bool isFirstLogin) => IsFirstLogin = isFirstLogin;
    }
}