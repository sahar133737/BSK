namespace BGSK1.Security
{
    internal static class CurrentUserContext
    {
        public static int UserId { get; set; }
        public static int RoleId { get; set; }
        public static string FullName { get; set; }
        public static string Email { get; set; }
        public static string RoleName { get; set; }
        public static string IpAddress { get; set; }
    }
}
