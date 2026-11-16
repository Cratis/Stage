// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_admitting_nested_scalar_rules : given.a_modeled_rule
{
    void Establish() => _source = "type Detail\n  name Name\n  next Detail optional\n" + Source
        .Replace("        validate", "        details Detail\n        validate", StringComparison.Ordinal)
        .Replace("          name not empty", "          details.next.name not empty", StringComparison.Ordinal);

    void Because() => Admit();

    [Fact] void should_preserve_rules_without_collection_traversal() => _refusal.ShouldBeNull();
}
