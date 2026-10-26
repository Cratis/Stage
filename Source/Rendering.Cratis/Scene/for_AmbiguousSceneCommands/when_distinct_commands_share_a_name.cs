// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_AmbiguousSceneCommands;

public class when_distinct_commands_share_a_name : a_two_module_request
{
    IReadOnlyList<string> _names = null!;
    string[] _keys = null!;
    string[] _expected = null!;

    void Establish() => DuplicateCommandName();
    void Because()
    {
        var slices = new SemanticApplicationContext(_request, _options).AllSlices;
        var index = new SceneCommandIndex(slices);
        _names = index.AmbiguousNames;
        _keys = [.. slices.SelectMany(slice => slice.Slice.Commands.Select(command => index.ById[command.Id].Key))];
        var structure = StructureConverter.Convert(_syntax);
        _expected = [.. slices.Where(slice => slice.Slice.Commands.Length > 0).Select(slice => structure.Modules.Single(module => module.Id == slice.Module.Name).Features.Single(feature => feature.Id == slice.FeaturePath.Single().Name).Slices.Single(node => node.Id == slice.Slice.Name).Command!)];
    }

    [Fact] void should_use_the_same_authored_names_as_the_syntax_structure() => _keys.ShouldEqual(_expected);
    [Fact] void should_produce_distinct_keys() => _keys.Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
    [Fact] void should_identify_the_ambiguous_authored_name() => _names.ShouldContainOnly("RegisterProject");
}
