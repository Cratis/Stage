// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers.given;

public class a_performer : Specification
{
    private protected SemanticImplementationAdmission.PerformerAdmission _admission = null!;

    protected string PerformerBody { get; set; } = "return readModels;";

    protected async Task Prepare(bool live = true, string cardinality = "[]", bool built = true, uint? resultVersion = null)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source(PerformerBody, live, cardinality, built));
        var request = Request(loaded);
        if (resultVersion is { } version)
        {
            request = request with { ImplementationRequirements = [request.ImplementationRequirements.Single() with { ResultVersion = version }] };
        }
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        _admission = SemanticImplementationAdmission.AdmitPerformer(context, context.Queries.Values.Single());
    }

    internal static string Source(string body, bool live = true, string cardinality = "[]", bool built = true) => $$"""
        module People
          feature Directory
            slice StateChange NamePerson
              command NamePerson
                personId Uuid identifier
                name String
                produces PersonNamed
                  for personId
                  personId = personId
                  name = name
              event PersonNamed
                personId Uuid
                name String
            slice StateView People
              readmodel PersonSummary
                personId Uuid
                name String
              query FindPeople => {{(live ? "observable " : string.Empty)}}PersonSummary{{cardinality}}
                {{(cardinality == "[]" ? "filter name String" : "by personId Uuid")}}
                performer
                  ```csharp
                  {{body}}
                  ```
        """ + (built ? "\n      projection PeopleProjection => PersonSummary\n        from PersonNamed key personId\n          personId = personId\n          name = name\n" : string.Empty);

    internal static ArtifactRenderRequest Request(LoadedSemanticModel loaded) =>
        new(loaded.Model, Screenplay.Semantics.Execution.SemanticExecutionPlan.Compile(loaded.Model).Plan, CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
}
