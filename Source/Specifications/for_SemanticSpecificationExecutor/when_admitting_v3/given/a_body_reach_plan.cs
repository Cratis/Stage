// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3.given;

public class a_body_reach_plan : Specification
{
    protected SemanticExecutionPlan _plan = null!;

    void Establish()
    {
        const string source = """
            concept Reviewed : String
            type Envelope
              tokens Reviewed[] optional
              next Envelope optional
            module Work
              feature Items
                slice StateChange Manage
                  command Clean
                    id String identifier
                    produces Cleaned
                      for id
                  command Feed
                    id String identifier
                    produces Changed
                      for id
                  command Validate
                    id String identifier
                    produces Cleaned
                      for id
                  command Review
                    id String identifier
                    token Reviewed
                    produces Cleaned
                      for id
                  command ReviewNested
                    id String identifier
                    envelopes Envelope[] optional
                    produces Cleaned
                      for id
                  event Cleaned
                  event Changed
                  specification Unrelated
                    when Clean
                      for "one"
                      id = "one"
                    then Cleaned
                      for "one"
                  specification Feeding
                    when Feed
                      for "one"
                      id = "one"
                    then Changed
                      for "one"
                slice StateView View
                  readmodel Snapshot
                    id String key
                  query SnapshotById => Snapshot?
                    by id String
                  reducer Fold => Snapshot
                    on Changed
                      ```csharp
                      return context.State;
                      ```
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("BodyReach"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("BodyReach", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var application = compilation.Value!.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        application = application with
        {
            Concepts = [application.Concepts.Single() with
            {
                Validations = [new(default, SemanticValidationRuleKind.RulePredicate, null, null) { Name = "CheckConcept", RequirementId = "concept-body" }]
            }],
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(command => command.Name == "Validate" ? command with { CodeValidations = [new("command-body")] } : command)]
            })] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(compilation.Value.Model.LanguageVersion, compilation.Value.Model.SemanticVersion, application)).Plan!;
    }
}
