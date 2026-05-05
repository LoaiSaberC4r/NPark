using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public class User : Entity<Guid>
    {
        private User()
        { }

        public string Name { get; private set; } = string.Empty!;
        public string Username { get; private set; } = string.Empty!;
        public string Email { get; private set; } = string.Empty!;
        public string PasswordHash { get; private set; } = string.Empty!;
        public string PhoneNumber { get; private set; } = string.Empty!;

        public string NationalId { get; private set; } = string.Empty!;
        public Guid RoleId { get; private set; }
        public Role Role { get; private set; } = null!;

        public TimeSpan? ShiftStartTime { get; private set; }
        public TimeSpan? ShiftEndTime { get; private set; }

        public static User Create(
            string name,
            string email,
            string username,
            string passwordHash,
            string phoneNumber,
            string nationalId,
            TimeSpan? shiftStartTime = null,
            TimeSpan? shiftEndTime = null)
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                Username = username,
                PhoneNumber = phoneNumber,
                NationalId = nationalId
            };

            user.SetShift(shiftStartTime, shiftEndTime);

            return user;
        }

        public void UpdateName(string name) => Name = name;

        public void UpdateEmail(string email) => Email = email;

        public void UpdateUserName(string username) => Username = username;

        public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;

        public void UpdatePhoneNumber(string phoneNumber) => PhoneNumber = phoneNumber;

        public void UpdateNationalId(string nationalId) => NationalId = nationalId;

        public void SetRole(Guid roleId) => RoleId = roleId;

        public void UpdatePassword(string passwordHash) => PasswordHash = passwordHash;

        public void SetShift(TimeSpan? shiftStartTime, TimeSpan? shiftEndTime)
        {
            ShiftStartTime = shiftStartTime;
            ShiftEndTime = shiftEndTime;
        }

        public void ClearShift()
        {
            ShiftStartTime = null;
            ShiftEndTime = null;
        }
    }
}