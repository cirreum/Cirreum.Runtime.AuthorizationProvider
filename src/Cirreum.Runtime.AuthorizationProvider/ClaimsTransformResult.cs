namespace Cirreum.AuthorizationProvider;

/// <summary>
/// Diagnostic record stashed in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>
/// after each claims transformation pass. Inspect via
/// <c>httpContext.Items[ClaimsTransformResult.ItemsKey]</c>
/// during debugging or in diagnostic middleware.
/// </summary>
/// <param name="Outcome">The transformation outcome (e.g. <c>RolesResolved</c>, <c>AlreadyTransformed</c>, <c>NoUserIdentifier</c>).</param>
/// <param name="ResolverType">The concrete <see cref="IRoleResolver"/> type name, if resolution was attempted.</param>
/// <param name="UserId">The external user identifier extracted from the token, if found.</param>
/// <param name="RoleClaimType">The claim type used for role claims (typically <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).</param>
/// <param name="RoleCount">The number of roles resolved and added, if any.</param>
public sealed record ClaimsTransformResult(
	string Outcome,
	string? ResolverType = null,
	string? UserId = null,
	string? RoleClaimType = null,
	int? RoleCount = null) {

	/// <summary>
	/// The <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> key used to store this result.
	/// </summary>
	public const string ItemsKey = "Cirreum.AuthorizationProvider.TransformResult";
}
