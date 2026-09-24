// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public class a_semantic_runtime : Specification
{
    protected ISemanticRuntime _runtime = null!;
    protected object _appender = null!;
    protected SemanticCommand _command = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _appender = appender;
        _runtime = SemanticRuntimeHosting.Create(plan.Plan!, appender);
        _command = plan.Plan!.Commands.Values.Single();
    }

    protected const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
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
                  registeredAt = $context.occurred
                  registeredBy = $context.causedBy.subject
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
                registeredAt DateTime
                registeredBy String
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
        """;
}
