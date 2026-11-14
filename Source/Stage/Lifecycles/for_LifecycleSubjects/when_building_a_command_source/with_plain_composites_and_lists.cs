// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_plain_composites_and_lists : given.a_subject
{
    JsonObject _result = null!;

    void Because()
    {
        var lines = _subject.Properties.Single(property => property.Name == "lines");
        var application = _plan.Model.Application;
        var type = application.Types.Single(type => type.Id == lines.Type.Target);
        var member = type.Properties.Single() with { Name = "SKU" };
        // The parser requires lower-case property initials; exercise the semantic converter's exact-name contract directly.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_plan.Model, application with { Types = [type with { Properties = [member] }] });
        var detail = lines with { Id = Id(103), Name = "Detail", Type = lines.Type with { IsCollection = false } };
        var labels = _status with { Id = Id(104), Name = "Labels", Type = _status.Type with { IsCollection = true } };
        Models(_subject with { Properties = [.. _subject.Properties, detail, labels] });
        _command = _command with { Properties = [lines with { Name = "Lines" }, detail, labels] };
        var item = SemanticValue.Composite([new(member.Id, SemanticValue.Text("A"))]);
        var empty = SemanticValue.Composite([new(member.Id, SemanticValue.Null)]);
        _result = LifecycleSubjects.CommandSource(_plan, _command,
        [
            new(lines.Id, SemanticValue.Array([item, empty, SemanticValue.Null])),
            new(detail.Id, empty),
            new(labels.Id, SemanticValue.Array([SemanticValue.Text("one"), SemanticValue.Null]))
        ]);
    }

    [Fact]
    void should_preserve_plain_typed_json_without_workflow_keys_or_camel_casing() =>
        JsonNode.DeepEquals(_result, JsonNode.Parse("""{"Lines":[{"SKU":"A"},{"SKU":null},null],"Detail":{"SKU":null},"Labels":["one",null]}""")).ShouldBeTrue();
}
