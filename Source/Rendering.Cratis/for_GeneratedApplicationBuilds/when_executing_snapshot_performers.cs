// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_executing_snapshot_performers : a_generated_application
{
    string _queryNamespace = null!;
    string _build = null!;
    string _test = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        const string source = """
            module People
              feature Directory
                slice StateView People
                  readmodel PersonSummary
                    personId Uuid
                    name String
                  query FindPeople => PersonSummary[]
                    filter name String
                    performer
                      ```csharp
                      return readModels.Where(row => row.Name == name && row.Name == context.Arguments.Name).Select(row => row with { Name = row.Name.ToUpperInvariant() });
                      ```
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SnapshotApp"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("people"), "people", "People.play", source);
        var compiled = new SemanticModelCompiler().Compile("SnapshotApp", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        var compilation = compiled.Value!;
        var model = compilation.Model;
        var options = new CratisRenderingOptions("SnapshotApp", "Snapshot");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("SnapshotApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(compilation.Documents, compiled.ImplementationRequirements),
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, options);
        _queryNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(context.ReadModels.Values.Single().Id).Path);
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    void Establish()
    {
        // Check the resolved compiler inputs, without injecting an analyzer reference into the scaffold.
        AddGeneratedHostSupport("Dependencies.props", """
            <Project>
              <Target Name="EnsureSnapshotQueryAnalyzer" BeforeTargets="CoreCompile">
                <ItemGroup>
                  <_ArcQueryAnalyzer Include="@(Analyzer)" Condition="'%(Analyzer.Filename)' == 'Cratis.Arc.Core.CodeAnalysis'" />
                </ItemGroup>
                <Error Condition="'@(_ArcQueryAnalyzer)' == ''" Text="Snapshot performer verification requires the Arc query analyzer." />
                <Error Condition="'$(RunAnalyzers)' == 'false' Or '$(RunAnalyzersDuringBuild)' == 'false'" Text="Snapshot performer verification requires analyzers to run." />
              </Target>
            </Project>
            """);
        AddGeneratedSpecification("snapshot_performers.cs", $$"""
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Security.Claims;
        using System.Threading.Tasks;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Queries;
        using Cratis.Arc.Tenancy;
        using Cratis.Chronicle.ReadModels;
        using NSubstitute;
        using Xunit;
        using {{_queryNamespace}};

        public class snapshot_performers
        {
            [Fact]
            public async Task filters_and_transforms_rows_through_the_snapshot_performer()
            {
                var first = new PersonSummary(Guid.NewGuid(), "Ada");
                var other = new PersonSummary(Guid.NewGuid(), "Grace");
                var last = new PersonSummary(Guid.NewGuid(), "Ada");
                var readModels = Substitute.For<IReadModels>();
                readModels.GetInstances<PersonSummary>().Returns(Task.FromResult<IEnumerable<PersonSummary>>([first, other, last]));
                var contexts = Substitute.For<IQueryContextManager>();
                contexts.Current.Returns(QueryContext.NotSet);
                var tenant = Substitute.For<ITenantIdAccessor>();
                tenant.Current.Returns(new TenantId("snapshot-tenant"));
                var principal = Substitute.For<ICurrentPrincipalAccessor>();
                principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));

                var result = (await PersonSummary.FindPeople(readModels, contexts, tenant, principal, "Ada")).ToArray();
                Assert.Equal(new[] { first.PersonId, last.PersonId }, result.Select(row => row.PersonId));
                Assert.All(result, row => Assert.Equal("ADA", row.Name));
                Assert.Empty(await PersonSummary.FindPeople(readModels, contexts, tenant, principal, "Missing"));
                Assert.DoesNotContain(readModels.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(IReadModels.Observe));
            }
        }
        """);
    }

    async Task Because()
    {
        _build = await Run("snapshot-performers-build.log", "build", "SnapshotApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _test = await Run("snapshot-performers-test.log", "test", "SnapshotApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact]
    public void should_build_without_arc0029_and_execute_the_snapshot_body()
    {
        Assert.Equal(string.Empty, BuildWarnings(_build));
        Assert.DoesNotContain("ARC0029", _build, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Passed!", _test, StringComparison.Ordinal);
    }
}
#endif
