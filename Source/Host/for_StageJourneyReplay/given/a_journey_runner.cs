// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Specifications;

namespace Cratis.Stage.Host.for_StageJourneyReplay.given;

public class a_journey_runner : Specification
{
    // Bodies pinned from the semantic host's real answers (for_SemanticHttpBinding's registration model).
    protected const string Accepted = """{"response":{"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","name":"Screenplay"},"correlationId":"ec033b1c-0a99-4fb3-8378-7c7accebf924","isSuccess":true,"isAuthorized":true,"isValid":true,"hasExceptions":false,"validationResults":[],"exceptionMessages":[],"exceptionStackTrace":"","authorizationFailureReason":""}""";
    protected const string Rejected = """{"correlationId":"00000000-0000-0000-0000-000000000000","isSuccess":false,"isAuthorized":true,"isValid":false,"hasExceptions":false,"validationResults":[{"severity":3,"message":"Project name is already in use","members":[],"reason":"constraintViolation","reasonDetail":"UniqueProjectName"}],"exceptionMessages":[],"exceptionStackTrace":"","authorizationFailureReason":""}""";
    protected const string Forbidden = """{"correlationId":"ec033b1c-0a99-4fb3-8378-7c7accebf924","isSuccess":false,"isAuthorized":false,"isValid":true,"hasExceptions":false,"validationResults":[],"exceptionMessages":[],"exceptionStackTrace":"","authorizationFailureReason":"Caller is not authorized."}""";
    protected const string Unsupported = """{"isSuccess":false,"isAuthorized":true,"isValid":true,"hasExceptions":true,"validationResults":[],"exceptionMessages":["Unsupported(ImplementationBody) ApplyLateFee: the reference executor does not run implementation bodies."]}""";
    protected const string AllProjects = """{"paging":{"page":0,"size":0,"totalItems":0,"totalPages":0},"correlationId":"ec033b1c-0a99-4fb3-8378-7c7accebf924","data":[{"id":"3fa85f64-5717-4562-b3fc-2c963f66afa6","projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","name":"Screenplay"}],"isSuccess":true,"isReady":true,"isAuthorized":true,"isValid":true,"hasExceptions":false,"validationResults":[],"exceptionMessages":[],"exceptionStackTrace":""}""";
    protected const string NoProjects = """{"paging":{"page":0,"size":0,"totalItems":0,"totalPages":0},"data":[],"isSuccess":true,"isReady":true,"isAuthorized":true,"isValid":true,"hasExceptions":false,"validationResults":[],"exceptionMessages":[],"exceptionStackTrace":""}""";

    protected readonly List<(string Method, string Uri, string Body)> _requests = [];
    private protected StageJourneyReport _report = null!;
    protected virtual TimeSpan StepTimeout => TimeSpan.FromSeconds(30);
    private protected virtual TimeProvider Time => TimeProvider.System;

    private protected StageRoutes Routes { get; } = new(
        new Dictionary<string, string>
        {
            ["Projects.RegisterProject"] = "/api/projects/register",
            ["RegisterProject"] = "/api/projects/register",
            ["Projects.ApplyLateFee"] = "/api/projects/apply-late-fee",
            ["Projects.Archive"] = "/api/projects/archive",
            ["Billing.Archive"] = "/api/billing/archive"
        },
        new Dictionary<string, string>
        {
            ["AllProjects"] = "/api/projects/all",
            ["LiveProjects"] = "/api/projects/live?waitForFirstResult=true"
        })
    {
        Diagnostics = ["GET 'Duplicate' is ambiguous between Billing.Duplicate, Projects.Duplicate; it is not resolved by name."]
    };

    private protected StageJourneyStepResult Result(int journey, int step) => _report.Journeys[journey].Steps[step];

    protected virtual Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body) => Task.FromResult(Answer(HttpStatusCode.OK, Accepted));

    protected static HttpResponseMessage Answer(HttpStatusCode status, string body, string? capability = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (capability is not null) response.Headers.Add("Stage-Unsupported-Capability", capability);

        return response;
    }

    protected async Task Replay(string journeys)
    {
        var parse = StageJourneyFile.Parse($$"""{ "schema": "stage-journey/v1", "journeys": {{journeys}} }""");
        if (parse.File is null) throw new InvalidJourneyFixture(string.Join(Environment.NewLine, parse.Errors));
        using var handler = new fake_handler(this);
        using var client = new HttpClient(handler);
        _report = await new StageJourneyRunner(Routes, client, new Uri("http://127.0.0.1:9090"), StepTimeout, Time).Run(parse.File.Journeys, CancellationToken.None);
    }

    sealed class fake_handler(a_journey_runner owner) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            owner._requests.Add((request.Method.Method, request.RequestUri!.PathAndQuery, body));

            return await owner.Respond(request, body).WaitAsync(cancellationToken);
        }
    }
}

public class InvalidJourneyFixture(string message) : Exception(message);
