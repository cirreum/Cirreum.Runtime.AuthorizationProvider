namespace Microsoft.Extensions.Hosting;

using Cirreum.AuthorizationProvider;
using Cirreum.AuthorizationProvider.Configuration;
using Cirreum.Logging.Deferred;
using Cirreum.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class HostApplicationBuilderExtensions {
	/// <summary>
	/// Register Authorization Provider.
	/// </summary>
	/// <typeparam name="TRegistrar">The type of the authorization provider registrar.</typeparam>
	/// <typeparam name="TSettings">The type of the provider settings.</typeparam>
	/// <typeparam name="TInstanceSettings">The type of the provider instance settings.</typeparam>
	/// <param name="builder">The host application builder.</param>
	/// <param name="authenticationBuilder">The authentication builder.</param>
	/// <returns>The host application builder for chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the configuration section exists but cannot be bound to <typeparamref name="TSettings"/>.
	/// </exception>
	public static IHostApplicationBuilder RegisterAuthorizationProvider<TRegistrar, TSettings, TInstanceSettings>(
		this IHostApplicationBuilder builder,
		AuthenticationBuilder authenticationBuilder)
		where TRegistrar : AuthorizationProviderRegistrar<TSettings, TInstanceSettings>, new()
		where TSettings : AuthorizationProviderSettings<TInstanceSettings>
		where TInstanceSettings : AuthorizationProviderInstanceSettings {

		var registrarName = typeof(TRegistrar).Name;
		var deferredLogger = Logger.CreateDeferredLogger();

		using (var loggingScope = deferredLogger.BeginScope(new { RegistrarName = registrarName })) {

			// Check if this specific registrar type is already registered
			if (builder.Services.IsMarkerTypeRegistered<TRegistrar>()) {
				deferredLogger.LogDebug(
					"Duplicate request for {RegistrarName} and will be skipped.",
					registrarName);
				return builder;
			}

			// Mark this registrar type as registered
			builder.Services.MarkTypeAsRegistered<TRegistrar>();

			var registrar = new TRegistrar();
			var providerSectionKey = GetProviderConfigPath(registrar.ProviderType, registrar.ProviderName);
			var providerSection = builder.Configuration.GetSection(providerSectionKey);

			if (!providerSection.Exists()) {
				deferredLogger.LogWarning(
					"No configuration settings found for {RegistrarName}.",
					registrarName);
				return builder;
			}

			var providerSettings = providerSection.Get<TSettings>()
				?? throw new InvalidOperationException(
					$"Invalid configuration for '{registrarName}' - section exists but cannot be bound to settings.");

			if (providerSettings.Instances.Count == 0) {
				deferredLogger.LogWarning(
					"No instances found to register for {RegistrarName}.",
					registrarName);
				return builder;
			}

			// Register the Provider
			registrar.Register(
				builder.Services,
				providerSettings,
				providerSection,
				authenticationBuilder);

			deferredLogger.LogDebug(
				"Registered {InstanceCount} provider instances for {RegistrarName} of type {ProviderType}.",
				providerSettings.Instances.Count,
				registrarName,
				registrar.ProviderType);
		}

		return builder;
	}

	// Helper method for building provider configuration paths
	private static string GetProviderConfigPath(ProviderType providerType, string providerName) =>
		$"Cirreum:{providerType}:Providers:{providerName}";
}