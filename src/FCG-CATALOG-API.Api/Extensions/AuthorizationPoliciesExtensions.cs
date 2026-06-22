using FCG_CATALOG_API.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace FCG_CATALOG_API.Api.Extensions
{
    public static class AuthorizationPoliciesExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
                .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole("Admin"))
                .AddPolicy(Policies.UserOrAdmin, policy => policy.RequireRole("User", "Admin"))
                .AddPolicy(Policies.UserOnly, policy => policy.RequireRole("User"));
            return services;
        }
    }
}
