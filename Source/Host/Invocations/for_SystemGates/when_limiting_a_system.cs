// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_SystemGates;

public class when_limiting_a_system : Specification
{
    Dictionary<string, int> _limits = null!;
    int _attemptsOfLimitedSystem;
    int _attemptsOfFreeSystem;
    Task _second = null!;
    bool _secondWaited;
    bool _secondPassed;

    async Task Because()
    {
        var options = new StageInvocationOptions
        {
            Systems =
            {
                ["Slack"] = new() { MaxConcurrent = 4, RequestsPerSecond = 1, MaxAttempts = 2 },
                ["Billing"] = new() { MaxConcurrent = 1 },
                ["Mail"] = new()
            }
        };
        _limits = [];
        SystemGates.Concurrency(options, _limits);
        using var gates = new SystemGates(Options.Create(options));
        _attemptsOfLimitedSystem = gates.Attempts(Describe("Slack", 5));
        _attemptsOfFreeSystem = gates.Attempts(Describe("Mail", 5));

        await gates.Pass("Slack", CancellationToken.None);
        _second = gates.Pass("Slack", CancellationToken.None);
        await Task.Delay(100);
        _secondWaited = !_second.IsCompleted;
        await gates.Pass("Mail", CancellationToken.None);
        await _second.WaitAsync(TimeSpan.FromSeconds(5));
        _secondPassed = _second.IsCompletedSuccessfully;
    }

    [Fact] void should_share_one_concurrency_gate_across_the_abilities_of_a_system() => _limits["ability:Slack.*"].ShouldEqual(4);
    [Fact] void should_gate_each_system_by_its_own_limit() => _limits["ability:Billing.*"].ShouldEqual(1);
    [Fact] void should_not_gate_a_system_without_a_concurrency_limit() => _limits.ContainsKey("ability:Mail.*").ShouldBeFalse();
    [Fact] void should_narrow_the_attempts_of_an_ability_to_the_limit_of_its_system() => _attemptsOfLimitedSystem.ShouldEqual(2);
    [Fact] void should_leave_the_attempts_alone_when_the_system_sets_no_limit() => _attemptsOfFreeSystem.ShouldEqual(5);
    [Fact] void should_hold_back_a_call_beyond_the_rate() => _secondWaited.ShouldBeTrue();
    [Fact] void should_let_it_through_once_there_is_room() => _secondPassed.ShouldBeTrue();

    static AbilityDescriptor Describe(string system, int attempts) =>
        new(new(system, "Post", 1), EffectClass.Safe, [], "sha256:x") { MaxAttempts = attempts };
}
