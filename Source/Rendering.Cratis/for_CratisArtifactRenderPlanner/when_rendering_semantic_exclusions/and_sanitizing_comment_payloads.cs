// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_sanitizing_comment_payloads : given.an_excluded_feature
{
    void Establish() => _request = _request with { Exclusions = [_exclusion with { Address = "Broken\r\nclass Injected {}\u0085\u2028\u2029", Diagnostics = [_exclusion.Diagnostics[0] with { Code = "PLAY0053\n#error Injected" }] }] };
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_emit_one_physical_comment_line() => Text(Comment).Count(character => character == '\n').ShouldEqual(1);
    [Fact] void should_remove_unicode_line_separators() => Text(Comment).Any(character => character is '\r' or '\u0085' or '\u2028' or '\u2029').ShouldBeFalse();
    [Fact] void should_not_use_the_address_as_a_path() => Comment.RelativePath.ShouldEqual("Projects/Registration/ScreenplayExclusions.g.cs");
    [Fact] void should_still_own_the_file_on_the_actual_parent() => Comment.Sources.Single().ShouldEqual(_feature.Id);
}
