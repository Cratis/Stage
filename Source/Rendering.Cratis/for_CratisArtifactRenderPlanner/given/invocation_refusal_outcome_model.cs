// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// A reaction whose invoked command, owned by another slice, refuses one value and has the refusal acknowledged, with specifications that assert both the invocation and that nothing is appended.
/// </summary>
/// <remarks>
/// Silent is correct. Succeeding never refuses and Mismatching states another command value, so each must fail an assertion.
/// </remarks>
internal static class invocation_refusal_outcome_model
{
    internal static ExecutableSemanticModel Create() => Create(Source);

    /// <summary>
    /// Creates the model from a source whose FollowUp slice invokes the Completion slice's command, stating the invocation each specification expects.
    /// </summary>
    /// <param name="source">The authored source.</param>
    /// <returns>The executable model.</returns>
    internal static ExecutableSemanticModel Create(string source)
    {
        var golden = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(34).Bytes.AsSpan());
        var application = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source).Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var command = feature.Slices.Single(candidate => candidate.Name == "Completion").Commands.Single();
        var id = command.Properties.Single(property => property.Name == "id");
        var slice = feature.Slices.Single(candidate => candidate.Name == "FollowUp");
        slice = slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                ThenInvocations = [new(command.Id, [new(id.Id, SemanticValue.Text(ExpectedId(specification.Name)))])]
            })]
        };
        application = application with { Modules = [module with { Features = [feature with { Slices = [slice, feature.Slices.Single(candidate => candidate.Name == "Completion")] }] }] };
        return ExecutableSemanticModel.Create(golden.LanguageVersion, golden.SemanticVersion, application);
    }

    static string ExpectedId(string specification) => specification switch
    {
        "Mismatching" => "other",
        "Succeeding" or "Recorded" => "fine",
        _ => "blocked"
    };

    internal const string Source = """
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
              specification Silent
                when append Started
                  for "blocked"
                  id = "blocked"
                then no events
              specification Succeeding
                when append Started
                  for "fine"
                  id = "fine"
                then no events
              specification Mismatching
                when append Started
                  for "blocked"
                  id = "blocked"
                then no events
            slice StateChange Completion
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
