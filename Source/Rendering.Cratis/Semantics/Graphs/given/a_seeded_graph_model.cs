// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;

internal static class a_seeded_graph_model
{
    internal static ArtifactRenderRequest Request()
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(38).Bytes.AsSpan());
        var options = new CratisRenderingOptions("Organization", "Organization");
        return new(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    internal static readonly string Source = an_organization_graph_model.Source.Replace("          name     PersonName\n        node Group", "          name     PersonName\n          tags     String[]\n          visits   Int optional\n        node Group", StringComparison.Ordinal) + "\n" + """
              specification KeepingASeededGraph
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada"
                    tags
                      item "first"
                      item "second"
                    visits = 2
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                    since = "2026-01-01T00:00:00Z"
                  has edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba"
                    role = "Lead \"team\""
                then graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada"
                    tags
                      item "first"
                      item "second"
                    visits = 2
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                    since = "2026-01-01T00:00:00Z"
                  has edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba"
                    role = "Lead \"team\""
              specification RevokingASeededRole
                given graph Organization
                  has edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba"
                    role = "Leader"
                when append RoleRevoked
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  assignmentId = "9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba"
                then graph Organization
                  no edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9d5d2f42-d1c9-4d54-bd8f-a257e7dc00ba"
              specification UpdatingASeededPerson
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Before"
                given MemberAdded
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                when append PersonRegistered
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name = "After"
                then graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "After"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
        """;

    internal static ArtifactRenderRequest Request(string source) => an_organization_graph_model.Request(source);

    internal static SemanticApplicationContext Context() => new(Request(), new("Organization", "Organization"));

    internal static SemanticApplicationContext Context(string source) => new(Request(source), new("Organization", "Organization"));
}
#endif
