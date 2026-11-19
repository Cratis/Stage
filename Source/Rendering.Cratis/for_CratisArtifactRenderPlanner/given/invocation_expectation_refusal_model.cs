// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// A reaction whose invoked command rejects a mapped value and handles the refusal in a branch, with specifications that assert only the invocation.
/// </summary>
internal static class invocation_expectation_refusal_model
{
    internal static ExecutableSemanticModel Create()
    {
        var golden = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(34).Bytes.AsSpan());
        var application = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(Source).Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var command = feature.Slices.Single(candidate => candidate.Name == "Completion").Commands.Single();
        var slice = feature.Slices.Single(candidate => candidate.Name == "FollowUp");
        var id = command.Properties.Single(property => property.Name == "id");
        slice = slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                ThenEvents = [],
                ThenErrors = [],
                ThenInvocations = [new(command.Id, [new(id.Id, SemanticValue.Text("blocked"))])]
            })]
        };
        application = application with { Modules = [module with { Features = [feature with { Slices = [slice, feature.Slices.Single(candidate => candidate.Name == "Completion")] }] }] };
        return ExecutableSemanticModel.Create(golden.LanguageVersion, golden.SemanticVersion, application);
    }

    const string Source = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
              reaction Worker
                runs as system
                when Started
                  invokes Complete
                    id = id
                    on refused by validation
                      acknowledge
                    on refused
                      acknowledge
              specification Blocked
                when append Started
                  for "blocked"
                  id = "blocked"
                then error "blocked"
            slice Automation Completion
              command Complete
                id String identifier
                validate
                  require id != "blocked"
                    message "blocked"
                produces Completed
                  for id
                  id = id
              event Completed
                id String
        """;
}
#endif
