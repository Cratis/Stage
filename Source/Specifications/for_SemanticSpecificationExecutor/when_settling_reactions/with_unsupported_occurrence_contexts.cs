// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_unsupported_occurrence_contexts : given.a_reaction_plan
{
    [Theory]
    [InlineData("$context.causedBy.name", StageExecutionCapability.Occurrence, "direct")]
    [InlineData("$context.causedBy.subject", StageExecutionCapability.Occurrence, "direct")]
    [InlineData("$context.causedBy.userName", StageExecutionCapability.Occurrence, "direct")]
    [InlineData("$context.causedBy.name", StageExecutionCapability.Occurrence, "refusal")]
    [InlineData("$context.causedBy.name", StageExecutionCapability.Occurrence, "invoke")]
    public async Task should_keep_audit_contexts_as_named_typed_refusals(string expression, StageExecutionCapability capability, string position)
    {
        var production = $"produces Followed\n  item = {expression}";
        var effect = position switch
        {
            "refusal" => "invokes Complete\n  item = \"\"\n  on refused by validation\n" + Indent(production, 4),
            "invoke" => $"invokes Complete\n  item = {expression}",
            _ => production
        };
        Compile(
            "reaction Worker\n  runs as system\n  when Started\n" + Indent(effect, 4),
            "then error",
            new(Action: "given clock \"2027-03-04T10:00:00.123Z\"\nwhen Begin\n  item = \"one\"\n  amount = 1", Authorization: "validate\n  item not empty message \"required\""));
        await Execute();
        _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        _result.Unsupported!.Capability.ShouldEqual(capability);
        _result.Unsupported.Details.ShouldContain("STAGE-ESM-013:");
    }

    static string Indent(string source, int spaces) => string.Join('\n', source.Split('\n').Select(line => new string(' ', spaces) + line));
}
