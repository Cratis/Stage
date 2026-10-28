// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Selects the repository root of a multi-service repository, the files each service rendered with
/// <see cref="CratisPlanOptions.ServiceLayout"/> leaves to it.
/// </summary>
/// <param name="RepositoryName">The repository name. It names the root solution and must be a dot-separated C# identifier.</param>
/// <param name="Services">The project names of the services, each a dot-separated C# identifier. <c language="text">Composition</c> is reserved for the Aspire application host.</param>
public sealed record CratisRepositoryPlanOptions(string RepositoryName, ImmutableArray<string> Services);
