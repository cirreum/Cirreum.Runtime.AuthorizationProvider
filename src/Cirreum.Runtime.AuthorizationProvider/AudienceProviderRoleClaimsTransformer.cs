namespace Cirreum.AuthorizationProvider;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

/// <summary>
/// ASP.NET <see cref="IClaimsTransformation"/> that enriches a principal authenticated
/// via an Audience-based authorization provider by resolving application roles and
/// adding them as <see cref="ClaimTypes.Role"/> claims.
/// </summary>
/// <remarks>
/// Invoked by ASP.NET's <see cref="IClaimsTransformation"/> pipeline after the
/// authentication handler produces a principal and before authorization policies
/// are evaluated. If the principal already contains role claims, no resolution occurs.
///
/// Role resolution is delegated to an <see cref="IRoleResolver"/> registered in DI.
/// Cirreum applications typically register this indirectly via
/// <c>CirreumAuthorizationBuilder.AddApplicationUserResolver&lt;T&gt;()</c>, which
/// provides an adapter that bridges <c>IApplicationUserResolver</c> to
/// <see cref="IRoleResolver"/>.
/// </remarks>
internal sealed partial class AudienceProviderRoleClaimsTransformer(
	IServiceProvider serviceProvider,
	IHttpContextAccessor httpContextAccessor,
	ILogger<AudienceProviderRoleClaimsTransformer> logger
) : IClaimsTransformation {

	private const string TransformedKey = "__Cirreum_AudienceProviderRoleClaimsTransformer";
	private const string RolesName = "roles";
	private const string RoleName = "role";
	private const string Oid = "oid";
	private const string Sub = "sub";
	private const string UserId = "user_id";

	static class WellKnownClaimTypes {
		/// <summary>Entra / Azure AD object identifier URI claim.</summary>
		public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
	}

	/// <summary>
	/// Transforms the specified ClaimsPrincipal by resolving application roles and adding
	/// them as role claims based on the user's identity.
	/// </summary>
	/// <remarks>This method checks if the ClaimsPrincipal has already been transformed to prevent duplicate
	/// transformations. It also handles various scenarios where the user identity may not be valid or roles cannot be
	/// resolved.</remarks>
	/// <param name="principal">The ClaimsPrincipal representing the user and their claims to be transformed.</param>
	/// <returns>A ClaimsPrincipal that includes the resolved role claims if any were found; otherwise, returns the original
	/// principal.</returns>
	public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) {
		var context = httpContextAccessor.HttpContext;
		if (context is null) {
			Log.NoHttpContext(logger);
			return principal;
		}

		using var activity = AuthorizationProviderDiagnostics.ActivitySource.StartActivity("ClaimsTransformation");
		activity?.SetTag("auth.transformer.name", "AudienceProviderRoleClaimsTransformer");

		if (context.Items.ContainsKey(TransformedKey)) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "already_transformed"));
			activity?.SetTag("auth.transform.outcome", "AlreadyTransformed");
			Log.AlreadyTransformed(logger);
			return Return(principal, context, "AlreadyTransformed");
		}

		// Mark immediately — prevents re-entry if ASP.NET calls TransformAsync again
		// on the same request before the async work completes.
		context.Items[TransformedKey] = true;

		var resolver = serviceProvider.GetService<IRoleResolver>();
		var resolverType = resolver?.GetType().Name;

		if (resolver is null) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "no_resolver"));
			activity?.SetTag("auth.transform.outcome", "NoResolver");
			Log.NoResolver(logger);
			return Return(principal, context, "NoResolver");
		}

		if (principal.Identity is not ClaimsIdentity identity) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "no_claims_identity"));
			activity?.SetTag("auth.transform.outcome", "NoClaimsIdentity");
			Log.NoClaimsIdentity(logger);
			return Return(principal, context, "NoClaimsIdentity", resolverType);
		}

		// Defensive: stamp the canonical scheme key for routes wired to an explicit scheme
		// that bypass the dynamic ForwardDefaultSelector (which is the primary writer).
		// TryAdd preserves the forward-selector's value when both run.
		// Key literal must match Cirreum.Security.AuthenticationContextKeys.AuthenticatedScheme
		// in Cirreum.Core — this package doesn't reference Cirreum.Core, so we duplicate the const.
		context.Items.TryAdd("__Cirreum_AuthenticatedScheme", identity.AuthenticationType);

		// Skip: workforce or internally-assigned user already has roles in the token.
		var roleClaimType = identity.RoleClaimType;
		activity?.SetTag("auth.role_claim_type", roleClaimType);
		if (ContainsRoles(identity, roleClaimType)) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "role_already_present"));
			activity?.SetTag("auth.transform.outcome", "RolesAlreadyPresent");
			Log.RolesAlreadyPresent(logger, roleClaimType);
			return Return(principal, context, "RolesAlreadyPresent", resolverType, roleClaimType: roleClaimType);
		}

		// Find the external ID claim (try several common claim types). If not present, we
		// can't resolve roles, so return the original principal.
		var userId = FindUserId(principal);
		if (userId is null) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "no_user_id"));
			activity?.SetTag("auth.transform.outcome", "NoUserIdentifier");
			Log.NoUserIdentifier(logger);
			return Return(principal, context, "NoUserIdentifier", resolverType, roleClaimType: roleClaimType);
		}
		activity?.SetTag("external.user.id", userId);

		// Resolve roles and add them as claims.
		try {

			var roles = await resolver.ResolveRolesAsync(userId, context.RequestAborted);

			if (roles is null or { Count: 0 }) {
				AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "no_roles_resolved"));
				Log.NoRolesResolved(logger, userId);
				activity?.SetTag("auth.transform.outcome", "NoRolesResolved");
				return Return(principal, context, "NoRolesResolved", resolverType, userId, roleClaimType);
			}

			// Add resolved roles as claims using the identity's RoleClaimType (usually ClaimTypes.Role).
			activity?.SetTag("auth.roles.count", roles.Count);
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "roles_resolved"));
			foreach (var role in roles) {
				identity.AddClaim(new Claim(roleClaimType, role));
			}

			if (logger.IsEnabled(LogLevel.Debug)) {
				var roleString = string.Join(", ", roles);
				Log.RolesResolvedDetail(logger, roleString, userId);
			}

			activity?.SetTag("auth.transform.outcome", "RolesResolved");
			Log.RolesResolved(logger, roles.Count, userId, roleClaimType);
			return Return(principal, context, "RolesResolved", resolverType, userId, roleClaimType, roles.Count);

		} catch (Exception e) {
			AuthorizationProviderDiagnostics.TransformCounter.Add(1, new KeyValuePair<string, object?>("outcome", "role_resolution_failed"));
			Log.RoleResolutionFailed(logger, e, userId);
			activity?.SetTag("auth.transform.outcome", "RoleResolutionFailed");
			return Return(principal, context, "RoleResolutionFailed", resolverType, userId, roleClaimType);
		}

	}

	private static bool ContainsRoles(ClaimsIdentity identity, string roleType) {

		foreach (var c in identity.Claims) {
			var t = c.Type;

			if (string.Equals(t, roleType, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(t, RolesName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(t, RoleName, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}

		return false;
	}

	private static string? FindUserId(ClaimsPrincipal principal) {
		foreach (var c in principal.Claims) {
			var t = c.Type;
			if (string.Equals(t, Oid, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(t, Sub, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(t, UserId, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(t, WellKnownClaimTypes.ObjectId, StringComparison.Ordinal)) {
				return c.Value;
			}
		}
		return null;
	}

	private static ClaimsPrincipal Return(
		ClaimsPrincipal principal,
		HttpContext context,
		string outcome,
		string? resolverType = null,
		string? userId = null,
		string? roleClaimType = null,
		int? roleCount = null) {
		context.Items[ClaimsTransformResult.ItemsKey] = new ClaimsTransformResult(outcome, resolverType, userId, roleClaimType, roleCount);
		return principal;
	}

	private static partial class Log {

		[LoggerMessage(EventId = 1000, Level = LogLevel.Trace, Message = "Claims transformation skipped because HttpContext was not available.")]
		public static partial void NoHttpContext(ILogger logger);

		[LoggerMessage(EventId = 1001, Level = LogLevel.Trace, Message = "Claims transformation skipped because the request was already transformed.")]
		public static partial void AlreadyTransformed(ILogger logger);

		[LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Claims transformation skipped because the principal identity was not a ClaimsIdentity.")]
		public static partial void NoClaimsIdentity(ILogger logger);

		[LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Claims transformation skipped because role claims already exist. RoleClaimType: {RoleClaimType}")]
		public static partial void RolesAlreadyPresent(ILogger logger, string roleClaimType);

		[LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Claims transformation skipped because no supported user identifier claim was found.")]
		public static partial void NoUserIdentifier(ILogger logger);

		[LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Role resolution failed for user identifier '{UserId}'.")]
		public static partial void RoleResolutionFailed(ILogger logger, Exception exception, string userId);

		[LoggerMessage(EventId = 1006, Level = LogLevel.Debug, Message = "No roles were resolved for user identifier '{UserId}'.")]
		public static partial void NoRolesResolved(ILogger logger, string userId);

		[LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Resolved {RoleCount} roles for user identifier '{UserId}' using role claim type '{RoleClaimType}'.")]
		public static partial void RolesResolved(ILogger logger, int roleCount, string userId, string roleClaimType);

		[LoggerMessage(EventId = 1008, Level = LogLevel.Debug, Message = "Resolved roles [{Roles}] for user identifier '{UserId}'.")]
		public static partial void RolesResolvedDetail(ILogger logger, string roles, string userId);

		[LoggerMessage(EventId = 1009, Level = LogLevel.Debug, Message = "Claims transformation skipped because no IRoleResolver is registered.")]
		public static partial void NoResolver(ILogger logger);
	}

}
