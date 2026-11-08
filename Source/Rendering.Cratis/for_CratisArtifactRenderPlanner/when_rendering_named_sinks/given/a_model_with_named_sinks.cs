// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;

/// <summary>
/// Compiles a directory of people whose listing is stored in a named read-model sink and whose organization graph is stored in a named graph sink.
/// </summary>
internal static class a_model_with_named_sinks
{
    internal const string Sinks = """
        sink Reporting for readmodels
        sink People for graphs

        """;

    internal const string Source = """
        concept PersonId   : Uuid
        concept PersonName : String

        module Organization
          feature People
            slice StateChange RegisterPerson
              command RegisterPerson
                personId PersonId identifier
                name     PersonName
                produces PersonRegistered
                  for personId
                  name = name
              event PersonRegistered
                name PersonName
          feature Chart
            slice StateView Directory
              readmodel Listing
                sink Reporting
                id   PersonId key
                name PersonName
              projection BuildListing => Listing
                from PersonRegistered
                  id   = $eventSourceId
                  name = name
            slice StateView OrganizationChart
              graph Organization
                sink People
                node Person
                  personId PersonId key
                  name     PersonName
              projection OrganizationChart => graph Organization
                node Person
                  from PersonRegistered key $eventSourceId
                    personId = $eventSourceId
                    name     = name
        """;

    /// <summary>
    /// The same model with every sink declaration and directive removed.
    /// </summary>
    internal static string WithoutSinks => Source
        .Replace("        sink Reporting\n", string.Empty, StringComparison.Ordinal)
        .Replace("        sink People\n", string.Empty, StringComparison.Ordinal);

    internal static SemanticCompilation Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Organization"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("organization"), "organization", "Organization.play", source);
        var result = new SemanticModelCompiler().Compile("Organization", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Value!;
    }

    internal static ArtifactRenderProfile Profile(SemanticCompilation compilation) =>
        CratisRendering.WithRealization(CratisRendering.CreateProfile(compilation.Model.Application.Name, new("Organization", "Organization")), compilation);

    internal static ArtifactRenderPlan Plan(SemanticCompilation compilation) =>
        new CratisArtifactRenderPlanner().Plan(new(compilation.Model, SemanticExecutionPlan.Compile(compilation.Model, []).Plan!, Profile(compilation), new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id)));

    /// <summary>
    /// Gets the digest of a profile's render inputs, as plan provenance records them.
    /// </summary>
    internal static string InputDigest(ArtifactRenderProfile profile) =>
        new ArtifactRenderInputProvenance([.. profile.Inputs.Select(input => new ArtifactRenderInputDigest(input.Name, input.Version, input.Sha256))], [], []).Digest;

    internal static string Text(ArtifactRenderPlan plan, string name) =>
        Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
#endif
