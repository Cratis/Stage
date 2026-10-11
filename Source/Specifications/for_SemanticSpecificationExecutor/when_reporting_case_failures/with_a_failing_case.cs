// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_reporting_case_failures;

public class with_a_failing_case : a_case_table
{
    async Task Because() => _result = (await new SemanticSpecificationExecutor().Run(_loaded.Plan, new([]), new() { SpecificationOrigins = _loaded.SpecificationOrigins })).Results.Single(result => result.Name == "Registering_Failing");

    [Fact] void should_fail_the_derived_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_name_the_case_and_authored_specification() => _result.Failures.Single().StartsWith("Case 'Failing' of 'Registering': ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_include_the_effective_input() => _result.Failures.Single().ShouldContain("when: name = \"\" (case)");
    [Fact] void should_include_the_effective_expectation() => _result.Failures.Single().ShouldContain("then error: message = \"wrong\" (case)");
    [Fact] void should_include_ordinary_fixture_values() => _result.Failures.Single().ShouldContain("when: projectId = \"project-1\" (authored)");
    [Fact] void should_match_screenplays_effective_fixture_text() => Fixtures(_result.Failures.Single()).ShouldEqual(Fixtures(new SemanticSpecificationRunner().Run(_compilation, SemanticId.Parse(_result.SpecificationId)).Failures.Single()));

    static string Fixtures(string failure) => failure[failure.IndexOf("Effective fixtures:", StringComparison.Ordinal)..];
}
