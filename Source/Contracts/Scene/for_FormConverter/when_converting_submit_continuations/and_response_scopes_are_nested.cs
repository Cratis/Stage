// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Interactions;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormConverter.when_converting_submit_continuations;

public class and_response_scopes_are_nested : Specification
{
    FormResponseBindings _scope = null!;

    void Establish() => _scope = new FormResponseBindings
    {
        Commands = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["Register"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["id"] = string.Empty, ["receipt"] = "receipt", ["internal$name"] = "internal" },
            ["Confirm"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["receipt"] = "receipt" }
        }
    }.For("Register");

    [Theory]
    [InlineData("item.id")]
    [InlineData("$route.id")]
    [InlineData("$id")]
    [InlineData("id + other")]
    [InlineData("ordinary")]
    public void should_leave_non_response_paths_alone(string path) => _scope.Value(new(path)).Kind.ShouldBeNull();

    [Theory]
    [InlineData(BindingSourceKind.DataContext)]
    [InlineData(BindingSourceKind.CommandResponse)]
    public void should_preserve_explicit_binding_kinds(BindingSourceKind kind) =>
        _scope.Value(new("id") { Kind = kind }).ShouldEqual(new BindingExpression("id") { Kind = kind });

    [Fact]
    public void should_resolve_a_bare_name_containing_an_internal_dollar() =>
        _scope.Value(new("internal$name")).Path.ShouldEqual("internal");

    [Fact]
    public void should_resolve_active_before_unavailable() =>
        _scope.For("Confirm").Value(new("receipt")).Kind.ShouldEqual(BindingSourceKind.CommandResponse);

    [Fact]
    public void should_preserve_a_literal_named_like_a_response()
    {
        var action = (NotifyAction)_scope.Action(new NotifyAction(NotificationLevel.Info, new("receipt")));
        action.Message.Text.ShouldEqual("receipt");
        action.Message.Binding.ShouldBeNull();
    }

    [Fact]
    public void should_shadow_return_names_with_form_fields_and_behavior_parameters() =>
        _scope.Without(["id"]).Value(new("id")).Kind.ShouldBeNull();

    [Fact]
    public void should_keep_shadowing_inside_nested_success() =>
        _scope.Without(["receipt"]).For("Confirm").Value(new("receipt")).Kind.ShouldBeNull();

    [Fact]
    public void should_inherit_the_response_through_confirmation()
    {
        var action = _scope.Action(new ConfirmAction(new("Continue?"))
        {
            OnSuccess = [new SetStateAction("selected", new("id"))]
        });
        ((SetStateAction)action.OnSuccess.Single()).Value.Kind.ShouldEqual(BindingSourceKind.CommandResponse);
    }

    [Fact]
    public void should_take_the_nearest_successful_command_response()
    {
        var action = _scope.Action(new ExecuteCommandAction("Confirm")
        {
            OnSuccess = [new SetStateAction("selected", new("receipt"))]
        });
        ((SetStateAction)action.OnSuccess.Single()).Value.Path.ShouldEqual("receipt");
    }

    [Fact]
    public void should_refuse_an_outer_capture_in_nested_success() =>
        Catch.Exception(() => _scope.Action(new ExecuteCommandAction("Confirm")
        {
            OnSuccess = [new SetStateAction("selected", new("id"))]
        })).ShouldBeOfExactType<UnsupportedFormResponseCapture>();

    [Fact]
    public void should_refuse_an_outer_capture_in_nested_failure() =>
        Catch.Exception(() => _scope.Action(new ExecuteCommandAction("Confirm")
        {
            OnFailure = [new SetStateAction("selected", new("receipt"))]
        })).ShouldBeOfExactType<UnsupportedFormResponseCapture>();

    [Fact]
    public void should_not_change_a_sibling_continuation_after_a_nested_command()
    {
        _scope.Action(new ExecuteCommandAction("Confirm"));
        _scope.Value(new("id")).Path.ShouldEqual(string.Empty);
    }
}
