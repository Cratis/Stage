// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_finalizing_the_external_base : an_application_with_external_bases
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_the_short_base() => Artifact("/when_registering.cs").ShouldContain("class when_registering : Specification");
    [Fact] void should_import_the_base_namespace() => Artifact("/when_registering.cs").ShouldContain("using Cratis.Specifications;");
    [Fact] void should_not_leave_the_base_qualified() => Artifact("/when_registering.cs").ShouldNotContain("global::Cratis.Specifications.Specification");
}
