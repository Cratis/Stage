// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_reporting_optional_mirror_issues : Specification
{
    StageStatus _status = null!;
    int _responseStatus;
    bool _nextCalled;
    readonly StageUnsupportedIssue _issue = new("ProjectionMirror", "projection-id", "Workbench mirror unavailable");

    async Task Because()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ISemanticRuntime)).Returns(Substitute.For<ISemanticRuntime>());
        _status = SemanticHost.RegistrationStatus(SemanticWorld.Empty, [], services, "Projects", "/tmp/stage-model", [_issue]);
        var pipeline = new ApplicationBuilder(services);
        SemanticHost.UseReadinessGate(pipeline, () => SemanticWorld.Empty, []);
        pipeline.Run(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/projects";
        await pipeline.Build()(context);
        _responseStatus = context.Response.StatusCode;
    }

    [Fact] void should_keep_the_semantic_host_ready() => _status.State.ShouldEqual("ready");
    [Fact] void should_keep_the_model_available() => _status.Model!.Name.ShouldEqual("Projects");
    [Fact] void should_expose_the_typed_mirror_issue() => _status.Issues!.Single().ShouldEqual(_issue);
    [Fact] void should_not_block_the_command_and_query_surface() => _nextCalled.ShouldBeTrue();
    [Fact] void should_not_return_an_unsupported_http_status() => _responseStatus.ShouldEqual(200);
}
