// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Engine.Guards;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Guards;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_supported_guarded_action_has_no_fallback : given.a_guarded_action
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    ExternalComponent _result = null!;
    ScreenGuardedActionSyntax _supported = null!;
    GuardedSelection _selection = null!;

    void Establish() => _supported = new("Process invoice",
        [new ScreenActionAlternativeSyntax(new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("draft", _location), _location), "SendInvoice", _location)],
        _location);

    void Because()
    {
        _result = (ExternalComponent)ScreenDirectiveConverter.Convert([_supported], "InvoiceDetails")[0];
        var properties = GuardedActionProperties.Read(_result);
        var alternatives = JsonSerializer.SerializeToElement(properties.Alternatives, _options);
        _selection = GuardedSelection.SelectAction(alternatives, JsonSerializer.SerializeToElement(new { status = "sent" }));
    }

    [Fact]
    void should_admit_the_supported_authored_action()
    {
        var admission = new SceneRuntimeAdmission();
        admission.VisitNode(_supported);
        admission.Issues.ShouldBeEmpty();
    }
    [Fact] void should_admit_the_serialized_action_without_an_otherwise() => SceneGuards.CanRunAction(_result.Properties).ShouldBeTrue();
    [Fact] void should_not_invent_an_otherwise() => _result.Properties.ContainsKey("otherwise").ShouldBeFalse();
    [Fact] void should_hide_when_no_alternative_matches() => _selection.Kind.ShouldEqual("hidden");
    [Fact] void should_select_no_command_or_navigation_branch() => _selection.Index.ShouldBeNull();
}
