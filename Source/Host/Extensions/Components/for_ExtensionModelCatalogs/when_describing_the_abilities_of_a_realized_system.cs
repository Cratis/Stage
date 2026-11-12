// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using Xunit;
using AnApplication = Cratis.Stage.Host.Extensions.given.an_extension_application;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionModelCatalogs;

public class when_describing_the_abilities_of_a_realized_system : AnApplication
{
    AbilityDescriptor _ability = null!;

    async Task Because()
    {
        await LoadApplication();
        _ability = ExtensionModelCatalogs.Abilities(_loaded.Model.Application, _modelPackages).Single();
    }

    [Fact] void should_describe_the_ability_of_the_contract() => (_ability.Reference.System, _ability.Reference.Ability).ShouldEqual(("Notifier", "Notify"));
    [Fact] void should_describe_the_effect_the_contract_declares() => _ability.Effect.ShouldEqual(EffectClass.Once);
    [Fact] void should_describe_its_outcomes() => _ability.Outcomes.ShouldContainOnly("Muted");
    [Fact] void should_answer_now() => _ability.AnswersLater.ShouldBeFalse();
}
