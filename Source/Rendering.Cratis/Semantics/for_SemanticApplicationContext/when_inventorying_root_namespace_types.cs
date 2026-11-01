// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext;

public class when_inventorying_root_namespace_types : a_profile_source_plan
{
    IReadOnlyList<GeneratedTypeName> _types = null!;

    void Because()
    {
        ArtifactRenderRequest request = new(_loaded.Model, _loaded.Plan, _profile, new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id));
        _types = new SemanticApplicationContext(request, new("Projects", "Projects")).GeneratedTypes;
    }

    [Fact] void should_catalog_strings_in_the_root_namespace() => Assert.Contains(new GeneratedTypeName("Projects", "GeneratedStrings"), _types);
}
