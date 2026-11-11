// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Configuration of the extension protocol, bound to <c lang="text">Stage:Extensions</c>. Nothing is listening and no extension is called unless it is enabled.
/// </summary>
public sealed class ExtensionOptions
{
    /// <summary>
    /// Gets the configuration section.
    /// </summary>
    public const string Section = "Stage:Extensions";

    /// <summary>
    /// Gets the version of the protocol Stage speaks. Backends with another major version are refused at the handshake.
    /// </summary>
    public static readonly (int Major, int Minor) ProtocolVersion = (1, 0);

    /// <summary>
    /// Gets or sets the port Stage's host services (settings, secrets, triggers, answers and telemetry) listen on. Zero disables the extension protocol.
    /// </summary>
    public int HostPort { get; set; }

    /// <summary>
    /// Gets or sets the address the host services listen on. It is loopback unless an extension runs in another container or machine.
    /// </summary>
    public string HostAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the address extensions are told to reach the host services on, when it differs from where they listen, such as a service name.
    /// Empty means <c lang="text">http://&lt;HostAddress&gt;:&lt;HostPort&gt;</c>.
    /// </summary>
    public string AdvertisedHostEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how long the handshake with a backend may take.
    /// </summary>
    public TimeSpan DescribeTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the backends by system name. This is where a backend that runs by itself is found; loading and activating packages
    /// replaces it by registering another <see cref="IExtensionEndpoints"/>.
    /// </summary>
    public IDictionary<string, ExtensionBackendOptions> Backends { get; set; } = new Dictionary<string, ExtensionBackendOptions>();

    /// <summary>
    /// Gets or sets how inbound traffic is stored and delivered.
    /// </summary>
    public InboundOptions Inbound { get; set; } = new();

    /// <summary>
    /// Gets whether the extension protocol is enabled.
    /// </summary>
    public bool Enabled => HostPort > 0;

    /// <summary>
    /// Gets the address the host services are advertised on.
    /// </summary>
    public string HostEndpoint => AdvertisedHostEndpoint.Length > 0 ? AdvertisedHostEndpoint : $"http://{HostAddress}:{HostPort}";
}

/// <summary>
/// Where one system's backend listens.
/// </summary>
public sealed class ExtensionBackendOptions
{
    /// <summary>
    /// Gets or sets the address of the backend's gRPC endpoint, such as <c lang="text">http://localhost:50051</c>.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}
