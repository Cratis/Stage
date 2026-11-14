// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_admitting_an_unused_enum : given.a_modeled_rule
{
    void Establish() => _source = "concept Unused : Enum\n  allowed\n" + Source;

    [Fact] void should_not_refuse_a_declaration_without_command_inputs() => _refusal.ShouldBeNull();
}
