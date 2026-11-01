// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using NSubstitute;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution.given;

public class a_model_root : Specification
{
    protected string root;
    protected string fingerprint = "first";
    protected IBodyWorkerClient compiler;
    protected IBodyWorkerClient worker;
    protected ModelRootBodyExecution execution;
    protected SemanticApplication application = new(default, "Application", [], [], []);
    protected static SemanticImplementationBody Body => new(new(SemanticImplementationRole.RulePredicate, default!, null, "csharp", null, Hash, null!) { RequirementId = "rule" }, "return true;");
    protected static string Hash => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("return true;")));
    protected static BodyUnitManifest Manifest => new("revision", [new("rule", "RulePredicate", true) { BodySha256 = Hash }]);
    protected static SemanticRuleContext Context => new(SemanticValue.Null, SemanticValue.Number(1), "amount", null, null, null);

    void Establish()
    {
        root = Directory.CreateTempSubdirectory("stage-model-root-").FullName;
        compiler = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        compiler.CompileAsync(root, Arg.Any<CancellationToken>()).Returns(Manifest);
        worker = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        worker.EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(new BodyEvaluation("satisfied"));
    }

    protected void Create() => execution = new(root, () => compiler, _ => worker, _ => fingerprint);
    protected ValueTask<ISemanticBodyExecutionProvider> Provider() => execution.CreateAsync(application, new Dictionary<string, SemanticImplementationBody> { ["rule"] = Body }, CancellationToken.None);

    async Task Destroy()
    {
        if (execution is not null) await execution.DisposeAsync();
        Directory.Delete(root, true);
    }
}
