// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Semantics;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.given;

public class a_bound_semantic_model : a_routed_model
{
    protected IAppendSemanticFacts _facts = null!;
    protected const string Route = "/api/projects/registration/register-project/register-project";
    protected const string Payload = "{\"projectId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"name\":\"Screenplay\"}";

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new Exception(string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        _facts = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_facts).Tail().Returns(ulong.MaxValue);
        if (!plan.Success)
        {
            throw new Exception(string.Join("; ", plan.Issues.Select(issue => issue.Details)));
        }
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan.Plan!, _facts);
    }

    const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        policy Staff
          require role "Staff"
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                validate
                  name not empty message "Project name is required"
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              command RestrictedProject
                projectId ProjectId identifier
                name ProjectName
                authorize Staff
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              command UnallocatedProject
                projectId ProjectId
                name ProjectName
                produces ProjectRegistered
                  projectId = projectId
                  name = name
              constraint UniqueProjectName
                unique name on ProjectRegistered
                message "Project name is already in use"
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
                authorize Staff
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
            slice StateView PublicLookup
              readmodel PublicSummary
                projectId ProjectId
                name ProjectName
              query PublicById => PublicSummary?
                by projectId ProjectId
              projection PublicSummaryProjection => PublicSummary
                from ProjectRegistered key projectId
                  name = name
        """;
}
