// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.given;

public class a_persona_bound_query : for_StageEndpointMapper.given.a_routed_model
{
    protected int _status;
    protected virtual bool Enabled => true;
    protected virtual string PersonaName => "Shopper";
    protected virtual string PersonaPolicy => "IsShopper";
    ClaimsPrincipal _persona = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("projects"), "projects", "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        plan.Success.ShouldBeTrue();
        _persona = SandboxPersona.Create(new ScreenplayCompiler().Compile(Source).Value!, NullLogger.Instance)!;
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan.Plan!, Substitute.For<IAppendSemanticFacts>());
    }

    protected override void ConfigureHostServices(IServiceCollection services)
    {
        SandboxIdentityOptions.Configure(services, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = Enabled.ToString() }).Build());
        services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
    }

    protected override void ConfigureHttpPipeline(IApplicationBuilder pipeline)
    {
        SandboxIdentity.Use(pipeline, _persona);
        pipeline.UseAuthentication();
    }

    string Source => $$"""
        concept ProjectId : Uuid
        policy IsShopper
          require role "Shopper"
        policy IsStaff
          require role "Staff"
        persona {{PersonaName}}
          policy {{PersonaPolicy}}
        module Projects
          feature Lookup
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name String
              query Projects => ProjectSummary[]
                authorize IsShopper
        """;
}
