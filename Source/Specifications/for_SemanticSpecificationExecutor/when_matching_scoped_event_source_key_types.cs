// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_matching_scoped_event_source_key_types : Specification
{
    const string Uuid = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01";

    [Theory]
    [InlineData("String", "one", false, true)]
    [InlineData("Uuid", Uuid, false, true)]
    [InlineData("Uuid", Uuid, true, true)]
    [InlineData("String", "one", false, false)]
    public async Task should_match_the_reference_for_compatible_keys(string primitive, string key, bool generated, bool passes)
    {
        var plan = Compile(primitive, primitive, key, generated, passes ? "recorded" : "wrong");
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed == passes, string.Join(';', reference.Failures));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == (passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed),
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.Failures.ShouldContainOnly(reference.Failures);
    }

    [Theory]
    [InlineData("String", "one")]
    [InlineData("Uuid", Uuid)]
    public async Task should_project_compatible_given_event_keys(string primitive, string key)
    {
        var plan = Compile(primitive, primitive, key, false, "recorded", given: true);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        result.Failures.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_match_the_reference_for_text_sources_holding_a_uuid(bool given)
    {
        var plan = Compile("String", "Uuid", Uuid, false, "recorded", given: given);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldBeTrue();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    [Fact]
    public async Task should_keep_literal_keys_unsupported_for_uuid_identifiers_without_misreporting_the_source_type()
    {
        var plan = Compile("Uuid", "Uuid", Uuid, false, "recorded", $"\"{Uuid}\"");
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())!.Details.ShouldEqual(
            "This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.ShouldEqual("This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.");
    }

    static SemanticExecutionPlan Compile(string sourceType, string identifierType, string key, bool generated, string expected, string projectionKey = "$eventSourceId", bool given = false)
    {
        var source = $$"""
            concept Key : {{identifierType}}
            {{(generated ? "  generate uuid" : string.Empty)}}
            module Work
              feature Work
                slice StateChange Recording
                  command Record
                    id {{(generated ? "Key generated" : sourceType)}} identifier
                    value String
                    produces Recorded
                      for id
                      value = value
                  event Recorded
                    value String
                  specification RecordIsProjected
            {{(generated ? $"        when Record\n          for \"{key}\"\n          value = \"recorded\"\n        then Recorded\n          for \"{key}\"\n          value = \"recorded\"" : $"        {(given ? "given" : "when append")} Recorded\n          for \"{key}\"\n          value = \"recorded\"")}}
                    then readmodel Current
                      id = "{{(projectionKey == "$eventSourceId" ? key : projectionKey.Trim('"'))}}"
                      value = "{{expected}}"
                slice StateView Current
                  readmodel Current
                    id Key key
                    value String
                  projection Build => Current
                    no automap
                    from Recorded{{(projectionKey == "$eventSourceId" ? string.Empty : $" key {projectionKey}")}}
                      id = {{projectionKey}}
                      value = value
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ScopedKeyTypes"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("ScopedKeyTypes", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var compiled = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.NotNull(compiled.Plan);
        return compiled.Plan;
    }
}
