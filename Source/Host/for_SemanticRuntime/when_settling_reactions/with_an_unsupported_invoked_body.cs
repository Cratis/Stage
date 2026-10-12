// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_unsupported_invoked_body : a_reaction_runtime
{
    protected override string Model => StartSource + InvokingSource.Replace("amount Decimal\n        produces", "amount Decimal\n        validate\n          ```csharp\n            return [];\n          ```\n        produces", StringComparison.Ordinal);

    async Task Because() => _result = await Execute();

    [Fact] void should_preserve_the_runtime_implementation_precheck() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_name_the_command_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Command);
    [Fact] void should_append_nothing() => _batches.ShouldBeEmpty();
    [Fact] void should_return_the_original_world() => _result.World.Facts.ShouldBeEmpty();
}
