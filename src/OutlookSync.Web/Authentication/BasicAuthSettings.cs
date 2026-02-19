namespace OutlookSync.Web.Authentication;

/// <summary>
/// Settings for HTTP basic authentication.
/// Configure in <c>appsettings.json</c> under the <c>BasicAuth</c> section, or via Docker environment
/// variables using double-underscore notation (e.g. <c>BasicAuth__Username</c>, <c>BasicAuth__Password</c>).
/// When <see cref="Username"/> is empty, basic authentication is disabled and all requests are allowed.
/// </summary>
/// <remarks>
/// <para>
/// In production, supply credentials through a secure mechanism such as Kubernetes Secrets,
/// Docker secrets, or a secrets manager (e.g. Azure Key Vault, AWS Secrets Manager) rather
/// than storing them in <c>appsettings.json</c> in plain text.
/// </para>
/// </remarks>
public sealed class BasicAuthSettings
{
    /// <summary>The configuration section name used to bind these settings.</summary>
    public const string SectionName = "BasicAuth";

    /// <summary>
    /// The username required for basic authentication.
    /// Leave empty to disable authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password required for basic authentication.
    /// Provide this value via a secure secret store in production environments.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
