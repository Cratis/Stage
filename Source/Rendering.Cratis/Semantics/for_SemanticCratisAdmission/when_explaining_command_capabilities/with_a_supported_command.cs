// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_supported_command : given.a_command_model
{
    void Because() => Evaluate();

    [Fact] void should_not_add_a_capability_diagnostic() => _diagnostics.ShouldBeEmpty();
}
