1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using System.Collections.Immutable;
5: using Cratis.Stage.Contracts.Rendering;
6: 
7: namespace Cratis.Stage.Rendering.Cratis.Semantics;
8: 
9: /// <summary>
10: /// Refuses each ESM v5–v10 construct the Cratis planner does not render yet, before any artifact is planned.
11: /// </summary>
12: /// <remarks>
13: /// Admitting a version by number never admits its constructs: a selected slice that uses one refuses the
14: /// whole plan with a diagnostic naming the construct, rather than rendering without it. A model that uses
15: /// none of them renders exactly as an earlier version would.
16: /// </remarks>
17: internal static class SemanticVersionFeatureAdmission
18: {
19:     /// <summary>
20:     /// Finds every unrendered v5–v10 construct in the selected scope.
21:     /// </summary>
22:     /// <param name="context">The indexed semantic application.</param>
23:     /// <param name="slices">The selected slices.</param>
24:     /// <returns>One blocking diagnostic per construct, in model order.</returns>
25:     public static ImmutableArray<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
26:     {
27:         var features = slices.Select(located => located.Slice).SelectMany(slice =>
28:             SemanticVersionFeatures.InSlice(slice).Except(SemanticVersionFeatures.ReducerFiltersInSlice(slice))
29:                 .Except(SemanticVersionFeatures.AutomationSlices(slice)).Except(SemanticVersionFeatures.EventReactionsInSlice(slice))
30:                 .Concat(SemanticVersionFeatures.EventReferences(slice, context.Events))
31:                 .Concat(slice.Commands.SelectMany(command => SemanticVersionFeatures.InCommand(command).Except(SemanticVersionFeatures.RoutesInCommand(command))
32:                     .Concat(SemanticVersionFeatures.UnsupportedGenerated(command, context.Application.Concepts))))
33:                 .Concat(slice.Specifications.SelectMany(specification => SemanticVersionFeatures.InSpecification(specification).Except(SemanticVersionFeatures.RoutesInSpecification(specification)))));
34: 
35:         // A scoped render also emits sibling constraints that govern its produced events.
36:         features = features.Concat(SemanticVersionFeatures.EventReferences(
37:             SemanticCratisAdmission.SelectedConstraints(context, slices).Select(selected => selected.Constraint), context.Events)).Distinct();
38: 
39:         // Application triggers belong to no slice; only an application render would otherwise drop them.
40:         if (context.Request.Scope.Kind == ArtifactRenderScopeKind.Application)
41:         {
42:             features = SemanticVersionFeatures.InApplication(context.Application).Except(SemanticVersionFeatures.RoutesInApplication(context.Application)).Concat(features);
43:         }
44: 
45:         return [.. features.Select(feature => new ArtifactRenderDiagnostic(feature.Code, ArtifactRenderDiagnosticSeverity.Error, feature.Message, feature.Artifact))];
46:     }
47: }
