// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_SystemActivationOverrides;

public class when_reading_the_activation_lines : Specification
{
    Dictionary<string, ActivationOverride> _overrides = null!;

    void Because()
    {
        _overrides = [];
        SystemActivationOverrides.Read(
            [
                "# a comment",
                "system Mail",
                "  extension \"acme/mail\"",
                "  activation always",
                "",
                "system Ledger",
                "  activation on demand idle after 10 minutes",
                "system Plain",
                "  activation on demand",
                "system Broken",
                "  activation sometimes",
                "  activation on demand idle after 0 minutes",
                "activation always"
            ],
            _overrides);
    }

    [Fact] void should_read_always() => _overrides["Mail"].Always.ShouldBeTrue();
    [Fact] void should_read_the_idle_timeout() => (_overrides["Ledger"].Always, _overrides["Ledger"].IdleAmount, _overrides["Ledger"].IdleUnit).ShouldEqual((false, 10, "minutes"));
    [Fact] void should_read_on_demand_without_a_timeout() => (_overrides["Plain"].Always, _overrides["Plain"].IdleAmount).ShouldEqual((false, null));
    [Fact] void should_not_read_what_is_not_an_activation_line() => _overrides.ContainsKey("Broken").ShouldBeFalse();
    [Fact] void should_not_read_a_line_outside_a_system() => _overrides.Count.ShouldEqual(3);
}
