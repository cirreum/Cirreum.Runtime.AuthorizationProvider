namespace Cirreum.AuthorizationProvider;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering audience-based role claims transformation.
/// </summary>
public static class AudienceRoleClaimsServiceCollectionExtensions {

	/// <summary>
	/// Registers the <see cref="AudienceProviderRoleClaimsTransformer"/> and its dependencies.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddAudienceRoleClaimsTransformation(this IServiceCollection services) {
		services.AddHttpContextAccessor();
		services.AddScoped<IClaimsTransformation, AudienceProviderRoleClaimsTransformer>();
		return services;
	}

}
