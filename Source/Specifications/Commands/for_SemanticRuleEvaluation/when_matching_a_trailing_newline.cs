// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_matching_a_trailing_newline : Specification
{
    SemanticCommand _command = null!;
    SemanticSpecification _specification = null!;
    string? _failure;

    void Establish()
    {
        const string source = "module Codes\n  feature Registration\n    slice StateChange Register\n      command Register\n        code String identifier\n        validate\n          code matches \"^[A-Z]{3}$\" message \"Three letters required\"\n        produces Registered\n          for code\n      event Registered\n      specification TrailingNewline\n        when Register\n          code = \"ABC\\n\"\n        then error \"Three letters required\"";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Codes"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("codes"), "codes", "Codes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Codes", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _command = plan.Commands.Values.Single();
        _specification = plan.Specifications.Values.Single();
    }

    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, []);

    [Fact] void should_reject_the_trailing_newline() => _failure.ShouldNotBeNull();
}
