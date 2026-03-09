namespace Cirreum.AuthorizationProvider;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Internal diagnostics for the Authorization Provider runtime.
/// Produces traces and metrics consumed by the OpenTelemetry pipeline.
/// </summary>
internal static class AuthorizationProviderDiagnostics {

	internal static readonly ActivitySource ActivitySource = new(AuthorizationDiagnostics.DiagnosticName);
	internal static readonly Meter Meter = new(AuthorizationDiagnostics.DiagnosticName);
	internal static readonly Counter<long> TransformCounter = Meter.CreateCounter<long>("auth_transformations_total");

}
