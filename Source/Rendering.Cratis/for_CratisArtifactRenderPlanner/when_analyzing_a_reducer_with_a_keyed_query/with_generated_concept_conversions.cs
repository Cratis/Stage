// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_analyzing_a_reducer_with_a_keyed_query;

public class with_generated_concept_conversions
{
    const string ConstructedBody = "return new ChangeRequest(TalkId: new global::Projects.Common.TalkId(Guid.Parse(context.Key)), Reason: context.Event.Reason, RequestedAt: context.Event.RequestedAt);";

    public static TheoryData<AdmittedConceptBody> AdmittedBodies => new()
    {
        new() { Self = false, Primitive = "String", Body = "return new ChangeRequest(context.Key, context.Key, context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "return new ChangeRequest(context.Key, context.Key, context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "String", Body = "return new ChangeRequest(TalkId: context.Key, Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "return new ChangeRequest(TalkId: context.Key, Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "Uuid", Body = "return new ChangeRequest(TalkId: Guid.Parse(context.Key), Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "Uuid", Body = "return new ChangeRequest(TalkId: Guid.Parse(context.Key), Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "String", Body = "global::Projects.Common.Reason reason = context.Key; global::Projects.Common.TalkId id = context.Key; return new ChangeRequest(TalkId: id, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "Reason reason = context.Key; TalkId id = context.Key; return new ChangeRequest(TalkId: id, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "Uuid", Body = "global::Projects.Common.TalkId id = Guid.Parse(context.Key); return new ChangeRequest(TalkId: id, Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "Uuid", Body = "TalkId id = Guid.Parse(context.Key); return new ChangeRequest(TalkId: id, Reason: context.Key, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "String", Body = "string? text = null; global::Projects.Common.Reason reason = text; return new ChangeRequest(TalkId: context.Key, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "string? text = null; Reason reason = text; return new ChangeRequest(TalkId: context.Key, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "String", Body = "global::Projects.Common.Reason reason = context.Key.Length > 0 ? null : \"text\"; return new ChangeRequest(TalkId: context.Key, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "Reason reason = context.Key.Length > 0 ? null : \"text\"; return new ChangeRequest(TalkId: context.Key, Reason: reason, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = false, Primitive = "String", Body = "string? s = null; string text = s!; global::Projects.Common.Reason r = text; return new ChangeRequest(TalkId: context.Key, Reason: r, RequestedAt: context.Event.RequestedAt);" },
        new() { Self = true, Primitive = "String", Body = "string? s = null; string text = s!; Reason r = text; return new ChangeRequest(TalkId: context.Key, Reason: r, RequestedAt: context.Event.RequestedAt);" }
    };

    [Theory]
    [MemberData(nameof(AdmittedBodies))]
    public async Task should_render_and_bind_the_identical_generated_operator_in_both_compilations(AdmittedConceptBody scenario)
    {
        var (self, primitive, body) = (scenario.Self, scenario.Primitive, scenario.Body);
        var source = with_a_body.Model(body, performer: false).Replace("concept TalkId : Uuid", $"concept TalkId : {primitive}", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var context = Context(loaded, self);
        var synthetic = Analyze(body, loaded, context);
        Assert.True(synthetic.Accepted, synthetic.Reason);
        var plan = when_rendering_a_pure_reducer.Plan(loaded, self: self);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var path = ReducerPath(plan, self);
        var files = Files(plan).ToArray();
        Assert.Contains(body, files.Single(file => file.RelativePath == path).Content, StringComparison.Ordinal);
        var compilation = RenderedOutput.CreateCompilation(files);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var rendered = PureTransitionAdmission.AnalyzeRendered(compilation, path, loaded.TypedContextDescriptors.Single(), context);
        Assert.True(rendered.Accepted, rendered.Reason);
        Assert.Equal(synthetic.ContextReads, rendered.ContextReads);
        Assert.Equal(synthetic.UsedAllowlistEntries, rendered.UsedAllowlistEntries);
        Assert.Equal(synthetic.BoundSymbols.ToArray(), rendered.BoundSymbols.ToArray());
        Assert.Equal(synthetic.AuditedSymbols.ToArray(), rendered.AuditedSymbols.ToArray());
        Assert.Contains(synthetic.BoundSymbols, identity => identity.Contains("Projects.Common.Reason.op_Implicit", StringComparison.Ordinal));
        Assert.Contains(synthetic.BoundSymbols, identity => identity.Contains("Projects.Common.TalkId.op_Implicit", StringComparison.Ordinal));
    }

    public static TheoryData<RefusedConceptOperation> RefusedOperations => new()
    {
        new() { Prefix = "string text = context.State.Reason;", Code = "STAGE-ESM-019", Message = "CS0029", ValueHint = true },
        new() { Prefix = "_ = context.State.Reason == \"text\";", Code = "STAGE-ESM-022", Message = "Symbol 'Projects.Common.Reason.operator ==", ValueHint = false },
        new() { Prefix = "global::Projects.Common.Reason reason = null;", Code = "STAGE-ESM-022", Message = "Conversion to 'Projects.Common.Reason?' is outside the pure allowlist.", ValueHint = false },
        new() { Prefix = "global::Projects.Common.Reason reason = default(string);", Code = "STAGE-ESM-022", Message = "Conversion to 'Projects.Common.Reason' is outside the pure allowlist.", ValueHint = false }
    };

    [Theory]
    [MemberData(nameof(RefusedOperations))]
    public async Task should_keep_refusing_other_concept_operations(RefusedConceptOperation scenario)
    {
        var (prefix, code, message, valueHint) = (scenario.Prefix, scenario.Code, scenario.Message, scenario.ValueHint);
        var body = prefix + " " + ConstructedBody;
        var loaded = await when_rendering_a_pure_reducer.Load(with_a_body.Model(body, performer: false));
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        var refusals = plan.Diagnostics.Where(candidate => candidate.Code == code && candidate.Message.Contains(message, StringComparison.Ordinal)).ToArray();
        Assert.True(refusals.Length == 1, string.Join(Environment.NewLine, plan.Diagnostics));
        var diagnostic = Assert.Single(refusals);
        if (code == "STAGE-ESM-022") Assert.Contains("is outside the pure allowlist.", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(valueHint, diagnostic.Message.Contains("Use .Value", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Diagnostics, candidate => candidate.Code == "STAGE-ESM-021");
    }

    [Fact]
    public async Task should_admit_explicit_value_reads_instead_of_concept_to_primitive_conversion()
    {
        var body = "string text = context.State.Reason.Value; " + ConstructedBody;
        var loaded = await when_rendering_a_pure_reducer.Load(with_a_body.Model(body, performer: false));
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    public static TheoryData<PeerConceptOperator> PeerOperators => new()
    {
        new() { Name = "Peer", Namespace = "Projects.Peers" },
        new() { Name = "Reason", Namespace = "Projects.Peers.Common" }
    };

    [Theory]
    [MemberData(nameof(PeerOperators))]
    public async Task should_not_trust_a_peer_operator_even_when_its_type_has_a_concept_name(PeerConceptOperator scenario)
    {
        var (name, peerNamespace) = (scenario.Name, scenario.Namespace);
        var loaded = await when_rendering_a_pure_reducer.Load(with_a_body.Model(ConstructedBody, performer: false));
        var context = Context(loaded, self: false);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var path = ReducerPath(plan, self: false);
        var body = $"global::{peerNamespace}.{name} value = context.Key; " + ConstructedBody;
        var files = Files(plan).Select(file => file.RelativePath == path
            ? file with { Content = file.Content.Replace(ConstructedBody, body, StringComparison.Ordinal) }
            : file);
        var compilation = RenderedOutput.CreateCompilation(files).AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            $"namespace {peerNamespace} {{ public record {name}(string Value) {{ public static implicit operator {name}(string value) => new(value); }} }}"));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var peer = compilation.GetTypeByMetadataName($"{peerNamespace}.{name}")!;
        var method = Assert.Single(peer.GetMembers("op_Implicit").OfType<IMethodSymbol>());
        Assert.False(PureTransitionAdmission.IsGeneratedConceptConversion(method, PureTransitionAdmission.GeneratedConceptSymbols(context, compilation)));
        var verdict = PureTransitionAdmission.AnalyzeRendered(compilation, path, loaded.TypedContextDescriptors.Single(), context);
        Assert.False(verdict.Accepted);
        Assert.Equal("STAGE-ESM-022", verdict.Code);
        Assert.Contains($"Conversion to '{peerNamespace}.{name}'", verdict.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_not_trust_inherited_concept_or_identifier_conversion_operators()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(with_a_body.Model(ConstructedBody, performer: false));
        var context = Context(loaded, self: false);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var compilation = RenderedOutput.CreateCompilation(Files(plan));
        var trusted = PureTransitionAdmission.GeneratedConceptSymbols(context, compilation);
        var operators = trusted.Keys.SelectMany(concept => concept.BaseType!.GetMembers().OfType<IMethodSymbol>())
            .Where(method => method.MethodKind == MethodKind.Conversion).ToArray();
        Assert.NotEmpty(operators);
        Assert.All(operators, method => Assert.False(PureTransitionAdmission.IsGeneratedConceptConversion(method, trusted)));
    }

    [Fact]
    public async Task should_keep_refusing_projection_source_concept_conversions()
    {
        const string safeBody = "return new ReducerTotal(Sku: context.Event.Sku, Stock: new global::Projects.Common.StockQuantity(0));";
        const string refusedBody = "global::Projects.Common.StockQuantity stock = context.Event.Quantity; return new ReducerTotal(Sku: context.Event.Sku, Stock: stock);";
        var source = when_executing_running_totals_of_concept_values.Source + "\n" + """
                slice StateView ReducedDemand
                  readmodel ReducerTotal
                    sku Sku key
                    stock StockQuantity
                  query ReducerTotalOfSku => ReducerTotal optional
                    by sku Sku
                  reducer Fold => ReducerTotal
                    on DemandRecorded
                      ```csharp
                      return new ReducerTotal(Sku: context.Event.Sku, Stock: new global::Projects.Common.StockQuantity(0));
                      ```
            """;
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var context = Context(loaded, self: false);
        var synthetic = Analyze(refusedBody, loaded, context);
        Assert.False(synthetic.Accepted);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var path = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).RelativePath;
        var files = Files(plan).Select(file => file.RelativePath == path
            ? file with { Content = file.Content.Replace(safeBody, refusedBody, StringComparison.Ordinal) }
            : file);
        var compilation = RenderedOutput.CreateCompilation(files);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var target = compilation.GetTypeByMetadataName("Projects.Common.StockQuantity")!;
        var method = Assert.Single(target.GetMembers("op_Implicit").OfType<IMethodSymbol>().Where(candidate => candidate.Parameters[0].Type.Name == "Quantity"));
        Assert.False(PureTransitionAdmission.IsGeneratedConceptConversion(method, PureTransitionAdmission.GeneratedConceptSymbols(context, compilation)));
        var rendered = PureTransitionAdmission.AnalyzeRendered(compilation, path, loaded.TypedContextDescriptors.Single(), context);
        Assert.False(rendered.Accepted);
        Assert.Equal("STAGE-ESM-022", rendered.Code);
    }

    public sealed record AdmittedConceptBody : IXunitSerializable
    {
        public bool Self { get; set; }
        public string Primitive { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Self), Self);
            info.AddValue(nameof(Primitive), Primitive);
            info.AddValue(nameof(Body), Body);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Self = info.GetValue<bool>(nameof(Self));
            Primitive = info.GetValue<string>(nameof(Primitive));
            Body = info.GetValue<string>(nameof(Body));
        }
    }

    public sealed record RefusedConceptOperation : IXunitSerializable
    {
        public string Prefix { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ValueHint { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Prefix), Prefix);
            info.AddValue(nameof(Code), Code);
            info.AddValue(nameof(Message), Message);
            info.AddValue(nameof(ValueHint), ValueHint);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Prefix = info.GetValue<string>(nameof(Prefix));
            Code = info.GetValue<string>(nameof(Code));
            Message = info.GetValue<string>(nameof(Message));
            ValueHint = info.GetValue<bool>(nameof(ValueHint));
        }
    }

    public sealed record PeerConceptOperator : IXunitSerializable
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Namespace), Namespace);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Name = info.GetValue<string>(nameof(Name));
            Namespace = info.GetValue<string>(nameof(Namespace));
        }
    }

    static SemanticApplicationContext Context(LoadedSemanticModel loaded, bool self)
    {
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan,
            self ? CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects")) : CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        return new(request, new("Projects", "Projects"));
    }

    static PureTransitionAdmission.Verdict Analyze(string body, LoadedSemanticModel loaded, SemanticApplicationContext context)
    {
        var reducer = context.Reducers.Single();
        return PureTransitionAdmission.Analyze(body, context, context.ReadModels[reducer.ReadModel],
            context.Events[reducer.Transitions.Single().EventContract], loaded.TypedContextDescriptors.Single(), loaded.ImplementationRequirements.Single());
    }

    static string ReducerPath(ArtifactRenderPlan plan, bool self) => plan.Artifacts.Single(artifact =>
        artifact.RelativePath.EndsWith(self ? "/ChangeRequests.cs" : "/OpenChangeRequests.cs", StringComparison.Ordinal)).RelativePath;

    static IEnumerable<RenderedFile> Files(ArtifactRenderPlan plan) => plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
#endif
