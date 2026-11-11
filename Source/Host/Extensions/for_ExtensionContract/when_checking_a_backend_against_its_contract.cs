// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Invocations;
using Xunit;
using EffectClass = Cratis.Stage.Host.Invocations.EffectClass;

namespace Cratis.Stage.Host.Extensions.for_ExtensionContract;

public class when_checking_a_backend_against_its_contract : Specification
{
    static readonly AbilityDescriptor[] _declared =
    [
        new(new("Slack", "Post", 1), EffectClass.Once, ["posted", "refused"], "sha256:a"),
        new(new("Slack", "Pin", 2), EffectClass.Idempotent, [], "sha256:a")
    ];

    IReadOnlyList<string> _faithful = null!;
    IReadOnlyList<string> _moreThanDeclared = null!;
    IReadOnlyList<string> _missing = null!;
    IReadOnlyList<string> _otherMajor = null!;
    IReadOnlyList<string> _otherEffect = null!;
    IReadOnlyList<string> _otherOutcomes = null!;

    void Because()
    {
        _faithful = ExtensionContract.Departures(_declared, Backend(Post("posted", "refused"), Pin(2)));
        _moreThanDeclared = ExtensionContract.Departures(_declared, Backend(Post("posted", "refused"), Pin(2), Ability("Extra", 1, Cratis.Stage.Extensions.Protocol.EffectClass.Safe)));
        _missing = ExtensionContract.Departures(_declared, Backend(Post("posted", "refused")));
        _otherMajor = ExtensionContract.Departures(_declared, Backend(Post("posted", "refused"), Pin(1)));
        _otherEffect = ExtensionContract.Departures(_declared, Backend(Ability("Post", 1, Cratis.Stage.Extensions.Protocol.EffectClass.Safe, "posted", "refused"), Pin(2)));
        _otherOutcomes = ExtensionContract.Departures(_declared, Backend(Post("posted"), Pin(2)));
    }

    static AbilityDeclaration Ability(string name, int major, Cratis.Stage.Extensions.Protocol.EffectClass effect, params string[] outcomes)
    {
        var declaration = new AbilityDeclaration { Name = name, ContractMajor = major, Effect = effect };
        declaration.Outcomes.AddRange(outcomes);
        return declaration;
    }

    static AbilityDeclaration Post(params string[] outcomes) => Ability("Post", 1, Cratis.Stage.Extensions.Protocol.EffectClass.Once, outcomes);

    static AbilityDeclaration Pin(int major) => Ability("Pin", major, Cratis.Stage.Extensions.Protocol.EffectClass.Idempotent);

    static ExtensionDescription Backend(params AbilityDeclaration[] abilities) => new("x", "1", [.. abilities], [], [], []);

    [Fact] void should_accept_a_backend_that_honors_the_contract() => _faithful.ShouldBeEmpty();
    [Fact] void should_accept_a_backend_that_realizes_more_than_the_contract() => _moreThanDeclared.ShouldBeEmpty();
    [Fact] void should_refuse_a_backend_that_leaves_an_ability_out() => _missing.Single().ShouldContain("ability:Slack.Pin@2 is not realized");
    [Fact] void should_refuse_a_backend_that_realizes_another_major() => _otherMajor.Single().ShouldContain("ability:Slack.Pin@2 is not realized");
    [Fact] void should_refuse_a_backend_that_changes_the_effect_class() => _otherEffect.Single().ShouldContain("declared Once and realized as Safe");
    [Fact] void should_refuse_a_backend_that_changes_the_outcomes() => _otherOutcomes.Single().ShouldContain("outcomes");
    [Fact] void should_map_an_unspecified_effect_to_the_strictest() => ExtensionContract.Effect(Cratis.Stage.Extensions.Protocol.EffectClass.Unspecified).ShouldEqual(EffectClass.Once);
}
