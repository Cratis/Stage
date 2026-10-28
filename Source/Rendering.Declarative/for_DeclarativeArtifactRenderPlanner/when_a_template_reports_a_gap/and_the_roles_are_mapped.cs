// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_a_template_reports_a_gap;

public class and_the_roles_are_mapped : Specification
{
    string[] _mapped = null!;

    void Because() => _mapped = [.. Enum.GetValues<GapRole>().Select(_ => DeclarativeRendering.Placeholder(_).ToString())];

    [Fact] void should_map_each_role_to_the_placeholder_of_the_same_name() => _mapped.ShouldEqual(Enum.GetNames<GapRole>());
    [Fact] void should_cover_every_placeholder_kind() => Enum.GetNames<ArtifactRenderPlaceholderKind>().Order().ShouldEqual(_mapped.Order());
}
