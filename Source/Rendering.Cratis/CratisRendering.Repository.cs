// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// Plans the repository root of a multi-service repository: the shared build files, a solution that lists every service and the
    /// composition, the docker-compose export, and an Aspire application host in <c language="text">Source/Composition/</c>.
    /// </summary>
    /// <param name="options">The repository name and the services, each rendered with <see cref="CratisPlanOptions.ServiceLayout"/>.</param>
    /// <returns>The repository artifacts and digest, or STAGE-PLAN-029 (invalid options) or STAGE-PLAN-032 (unknown profile version) and no artifacts.</returns>
    public static CratisPlanResult PlanRepository(CratisRepositoryPlanOptions options)
    {
        var invalid = CratisRepositoryScaffold.Validate(options);
        if (invalid is not null)
        {
            return CratisPlanResult.Create(options?.RepositoryName ?? string.Empty, [], [CratisPlanResult.Error("STAGE-PLAN-029", invalid)]);
        }

        if (CratisBackendApplicationScaffoldProfile.ForVersion(options.ProfileVersion) is not { } profile)
        {
            return CratisPlanResult.Create(options.RepositoryName, [], [CratisPlanResult.Error("STAGE-PLAN-032", $"The scaffold profile version '{options.ProfileVersion}' is not supported.")]);
        }

        return CratisPlanResult.Create(options.RepositoryName, [.. CratisRepositoryScaffold.Render(options, profile)], []);
    }
}
