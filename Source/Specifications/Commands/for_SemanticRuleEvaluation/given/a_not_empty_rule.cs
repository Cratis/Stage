// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.given;

public class a_not_empty_rule : Specification
{
    protected SemanticCommand _command = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticApplication _application = null!;
    protected string? _failure;

    protected void Prepare(string value, bool concept = false, bool collection = false)
    {
        var type = collection ? "String[]" : "String";
        if (concept) type = "Name";
        var source = (concept ? "concept Name : String\n  validate\n    not empty message \"Name is required\"\n" : string.Empty) + $"""
            module Names
              feature Registration
                slice StateChange Register
                  command Register
                    id String identifier
                    name {type}
            """;
        if (!concept) source += "\n        validate\n          name not empty message \"Name is required\"";
        source += $"\n        produces Registered\n          for id\n      event Registered\n      specification CheckingName\n        when Register\n          id = \"name-1\"\n          name = {value}\n        then Registered\n          for \"name-1\"";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Names"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("names"), "names", "Names.play", source);
        var compilation = new SemanticModelCompiler().Compile("Names", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _command = plan.Commands.Values.Single();
        _specification = plan.Specifications.Values.Single();
        _application = plan.Model.Application;
    }
}
