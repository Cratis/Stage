// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_described_command_handlers : Specification
{
    readonly DefaultHttpContext _http = new();
    IAppendSemanticFacts _appender = null!;
    ISemanticRuntime _runtime = null!;
    SemanticCommand _command = null!;
    CommandResult _denied = null!;
    CommandResult _invalid = null!;
    CommandResult _body = null!;

    void Establish()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan()), []).Plan;
        _command = plan.Commands.Values.Single(command => command.Handler is not null);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _appender = appender;
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
    }

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Role, "Accountant")], "specification"));
        _denied = await Execute(new ClaimsPrincipal(), 3);
        _invalid = await Execute(principal, 0);
        _body = await Execute(principal, 3);
    }

    [Fact] void should_deny_before_reaching_the_body() => _denied.IsAuthorized.ShouldBeFalse();
    [Fact] void should_validate_before_reaching_the_body() => _invalid.ValidationResults.Single().Message.ShouldEqual("Invoice must be overdue");
    [Fact] void should_report_the_reached_body() => _body.ExceptionMessages.Single().ShouldContain("Unsupported(ImplementationBody)");
    [Fact] void should_set_the_capability_header() => _http.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("ImplementationBody");
    [Fact] void should_set_the_artifact_header() => _http.Response.Headers["Stage-Unsupported-Artifact"].ToString().ShouldEqual(_command.Name);
    [Fact] void should_preserve_the_reference_message_without_a_stage_prefix() => _body.ExceptionMessages.Single().ShouldContain($"Command 'ApplyLateFee' is described but not implemented (requirement {_command.Handler.RequirementId}); its handler body requires a target.");
    [Fact] async Task should_never_append_handler_facts() => await _appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    [Fact]
    public async Task should_run_all_seven_specifications_through_the_isolated_host_endpoint()
    {
        var corpus = DescribedCommandHandlersCorpus.V23;
        var directory = Path.Combine(Path.GetTempPath(), $"stage-described-handlers-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, $"{corpus.ApplicationName}.play");
            await File.WriteAllTextAsync(path, corpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Text, TestContext.Current.CancellationToken);
            var plan = (await SemanticModelLoader.LoadFromPathAsync(path)).Plan;
            var builder = WebApplication.CreateBuilder();
            builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
            builder.Services.AddSingleton<ISpecificationRunProcess>(new SpecificationRunProcess());
            builder.Services.AddSingleton(new SemanticSpecificationProcessOptions(Path.Combine(AppContext.BaseDirectory, "specrunner", "Cratis.Stage.SpecRunner.dll"), TimeSpan.FromSeconds(120)));
            await using var app = builder.Build();
            SemanticSpecificationRuns.Map(app, plan, path);
            var pipeline = new ApplicationBuilder(app.Services);
            pipeline.UseRouting();
            pipeline.UseEndpoints(endpoints =>
            {
                foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
            });
            await using var scope = app.Services.CreateAsyncScope();
            await using var requestBody = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await using var responseBody = new MemoryStream();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new body_detection());
            context.Request.Method = "POST";
            context.Request.Path = SemanticSpecificationRuns.Route;
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = requestBody.Length;
            context.Request.Body = requestBody;
            context.Response.Body = responseBody;
            await pipeline.Build()(context);
            context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
            var report = SemanticSpecificationRunReportFile.Read(Encoding.UTF8.GetString(responseBody.ToArray()))!;
            report.Results.Count.ShouldEqual(7);
            foreach (var expectation in corpus.SpecificationExpectations)
            {
                var specification = plan.Specifications.Values.Single(specification => specification.Name == expectation.Name);
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                var host = report.Results.Single(result => result.SpecificationId == specification.Id.ToString());
                reference.Passed.ShouldEqual(expectation.Passed);
                reference.Execution.Kind.ShouldEqual(expectation.Outcome);
                reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                host.Outcome.ShouldEqual(expectation.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Unsupported);
                if (expectation.Passed) host.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
                if (reference.Execution is SemanticUnsupported unsupported)
                {
                    unsupported.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
                    host.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.ImplementationBody);
                    host.Unsupported.Details.ShouldEqual($"STAGE-IMPL-002: {unsupported.Details}");
                }
                else if (reference.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                    host.Trace!.Rejection.ShouldEqual(rejected.Details);
                }
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    async Task<CommandResult> Execute(ClaimsPrincipal principal, int days)
    {
        var payload = new Dictionary<string, JsonElement> { ["invoiceId"] = JsonSerializer.SerializeToElement("invoice-1"), ["days"] = JsonSerializer.SerializeToElement(days) };
        var result = await _runtime.Execute(_command, payload, principal, new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);

        return SemanticCommandOutcome.Map(result, CorrelationId.New(), _http, _command.Name);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();

    sealed class body_detection : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
