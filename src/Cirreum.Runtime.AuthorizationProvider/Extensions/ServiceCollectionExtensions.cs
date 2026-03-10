namespace Cirreum.AuthorizationProvider;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering role enrichment services.
/// </summary>
public static class RoleEnrichmentServiceCollectionExtensions {

	/// <summary>
	/// Registers the role enrichment claims transformer and its dependencies.
	/// Called by <c>CirreumAuthorizationBuilder.AddRoleResolver</c> in the Runtime Extensions layer.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddRoleEnrichment(this IServiceCollection services) {
		services.AddHttpContextAccessor();
		services.AddScoped<IClaimsTransformation, AudienceProviderRoleClaimsTransformer>();
		return services;
	}

}