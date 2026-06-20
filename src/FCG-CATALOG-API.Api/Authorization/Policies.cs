namespace FCG_CATALOG_API.Api.Authorization
{
    public static class Policies
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string UserOnly = nameof(UserOnly);
        public const string UserOrAdmin = nameof(UserOrAdmin);
    }
}
