// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host;

/// <summary>
/// Owns a startup snapshot shared by admission, Arc mapping, and introspection.
/// </summary>
/// <param name="enableQueryHttpMethod">Whether the configured alternate query transport is enabled.</param>
internal sealed class StageHttpRouteOptions(bool enableQueryHttpMethod = true)
{
    internal ApiEndpointOptions Canonical { get; } = new()
    {
        RoutePrefix = "api",
        SegmentsToSkipForRoute = 1,
        IncludeCommandNameInRoute = true,
        IncludeQueryNameInRoute = true,
        EnableQueryHttpMethod = enableQueryHttpMethod
    };

    internal ApiEndpointOptions Legacy { get; } = new()
    {
        RoutePrefix = "api",
        SegmentsToSkipForRoute = 1,
        IncludeCommandNameInRoute = false,
        IncludeQueryNameInRoute = true,
        EnableQueryHttpMethod = enableQueryHttpMethod
    };

    internal static StageHttpRouteOptions FromConfiguration(IConfiguration configuration)
    {
        // Match pinned AddCratisArc's default configuration section without constructing Arc or its providers.
        // Stage owns route shape, but preserves the deployment's existing opt-out from the QUERY transport.
        var section = configuration.GetSection(ConfigurationPath.Combine(Cratis.Arc.HostBuilderExtensions.DefaultSectionPaths))
            .GetSection(nameof(ArcOptions.GeneratedApis));
        var configured = section.Get<ApiEndpointOptions>() ?? new ApiEndpointOptions();
        return new StageHttpRouteOptions(configured.EnableQueryHttpMethod);
    }

    internal static void AlignIntrospection(IServiceCollection services) =>
        services.AddSingleton<IOptions<ApiEndpointOptions>>(provider =>
            Options.Create(provider.GetRequiredService<IOptions<ArcOptions>>().Value.GeneratedApis));
}
