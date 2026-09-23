// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_InteractionActionConverter.when_converting_a_message;

/// <summary>
/// The three forms stay distinguishable all the way to the renderer. Collapsed into one string, a missing
/// translation would be indistinguishable from a deliberate literal.
/// </summary>
public class and_it_was_authored_in_each_form : Specification
{
    SceneModel.InteractionMessage _literal = null!;
    SceneModel.InteractionMessage _stringsReference = null!;
    SceneModel.InteractionMessage _binding = null!;

    void Because()
    {
        _literal = InteractionActionConverter.ConvertMessage("Are you sure?", true);
        _stringsReference = InteractionActionConverter.ConvertMessage("$strings.confirmCancel", false);
        _binding = InteractionActionConverter.ConvertMessage("state.message", false);
    }

    [Fact] void should_keep_a_literal_as_text() => _literal.Text.ShouldEqual("Are you sure?");
    [Fact] void should_not_treat_a_literal_as_a_key() => _literal.StringsKey.ShouldBeNull();
    [Fact] void should_strip_the_prefix_from_a_strings_reference() => _stringsReference.StringsKey.ShouldEqual("confirmCancel");
    [Fact] void should_not_treat_a_strings_reference_as_text() => _stringsReference.Text.ShouldBeNull();
    [Fact] void should_treat_anything_else_as_a_binding() => _binding.Binding!.Path.ShouldEqual("state.message");
    [Fact] void should_not_treat_a_binding_as_text() => _binding.Text.ShouldBeNull();
}
