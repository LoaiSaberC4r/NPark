namespace NPark.Domain.FileNames
{
    public static class AppRoleIds
    {
        public static readonly Guid Admin =
            new("f47ac10b-58cc-4372-a567-0e02b2c3d479");

        public static readonly Guid EntranceCashier =
            new("11111111-1111-1111-1111-111111111111");

        public static readonly Guid ExitCashier =
            new("22222222-2222-2222-2222-222222222222");

        public static readonly Guid Supervisor =
            new("3f47ac10-b58c-4372-a567-0e02b2c3d480");

        public static bool IsCashier(Guid roleId)
        {
            return roleId == EntranceCashier ||
                   roleId == ExitCashier;
        }

        public static bool IsNonCashierSystemUser(Guid roleId)
        {
            return roleId == Admin ||
                   roleId == Supervisor;
        }
    }
}