// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_compiling_capture_members : given.a_translation_application
{
    [Theory]
    [InlineData("Observe")]
    [InlineData("Previous")]
    [InlineData("Overlay")]
    [InlineData("SameState")]
    [InlineData("Changed")]
    [InlineData("Text")]
    [InlineData("ConceptValue")]
    [InlineData("Field")]
    [InlineData("Same")]
    [InlineData("MissingCaptureField")]
    [InlineData("EmptyCaptureKey")]
    [InlineData("OnShipmentDispatched")]
    [InlineData("@Observe")]
    [InlineData("Obser\\u0076e")]
    [InlineData("Obs\u200cerve")]
    [InlineData("_12Capture")]
    [InlineData("@eventStore")]
    [InlineData("@class")]
    [InlineData("12Capture")]
    [InlineData("---")]
    [InlineData("same_state")]
    [InlineData("Δelta")]
    [InlineData("Capture")]
    public void should_compile_the_actual_capture_with_its_modeled_type_name(string name)
    {
        _model = Transform(_model, slice => slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { Name = name })]
        });
        Render(clientOnly: true);
        Assert.NotNull(Generated("TrackShipments", SemanticCaptureIdentifier.Decode(Identifiers.ToPascalCase(name))));
        Assert.Empty(RenderedOutput.Warnings(_sources));
    }

    [Theory]
    [InlineData("@Observe", "Observe")]
    [InlineData("Obser\\u0076e", "Observe")]
    [InlineData("Obs\u200cerve", "Observe")]
    [InlineData("@class", "class")]
    [InlineData("Δelta", "Δelta")]
    [InlineData("É", "É")]
    [InlineData("E\u0301", "E\u0301")]
    public void should_match_compiler_symbol_identity_without_case_or_unicode_normalization(string spelling, string identity)
    {
        var tree = CSharpSyntaxTree.ParseText($"class {spelling} {{ }}", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Identifier", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], new(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single(), TestContext.Current.CancellationToken);
        Assert.Equal(identity, symbol!.Name);
        Assert.True(SemanticCaptureIdentifier.TryDecode(spelling, out var decoded));
        Assert.Equal(symbol.Name, decoded);
    }

    [Fact]
    public void should_reserve_every_preference_before_allocating_a_fallback()
    {
        var allocator = new SemanticCaptureMemberAllocator(["Observe", "Observe_", "Observe_2"], ["@Observe"]);
        Assert.Equal("Observe_3", allocator.Allocate("Observe"));
        Assert.Equal("Observe_", allocator.Allocate("Observe_"));
        Assert.Equal("Observe_2", allocator.Allocate("Observe_2"));
    }

    [Fact]
    public void should_keep_default_generated_spellings_and_output_deterministic()
    {
        var first = Plan(_model);
        var second = Plan(_model);
        Assert.Equal(first.Artifacts.Select(artifact => artifact.RelativePath), second.Artifacts.Select(artifact => artifact.RelativePath));
        Assert.Equal(first.Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())), second.Artifacts.Select(artifact => Convert.ToHexString(artifact.Bytes.AsSpan())));
        Render();
        var capture = Generated("TrackShipments", "ShipmentTracking");
        Assert.NotNull(capture.GetMethod("OnShipmentDispatched"));
        Assert.Equal("eventStore", capture.GetConstructors().Single().GetParameters().Single().Name);
        foreach (var name in new[] { "Previous", "Observe", "Overlay", "SameState", "Changed", "Text", "ConceptValue", "Field", "Same" })
        {
            Assert.NotNull(capture.GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance));
        }
    }

    [Theory]
    [InlineData("@class")]
    [InlineData("ShipmentDispat\\u0063hed")]
    [InlineData("ShipmentDispat\u200cched")]
    public void should_compile_handlers_from_decoded_source_identifiers(string name)
    {
        _model = Transform(_model, slice => slice with { Events = [.. slice.Events.Select(contract => contract.Name == "ShipmentDispatched" ? contract with { Name = name } : contract)] });
        Render();
        Assert.NotNull(Generated("TrackShipments", "ShipmentTracking").GetMethod($"On{SemanticCaptureIdentifier.Decode(Identifiers.ToPascalCase(name))}"));
        Assert.Empty(RenderedOutput.Warnings(_sources));
    }

    [Theory]
    [InlineData(SemanticCaptureConditionKind.Added, "Previous")]
    [InlineData(SemanticCaptureConditionKind.AnyChanged, "Changed")]
    [InlineData(SemanticCaptureConditionKind.AllChanged, "Field")]
    [InlineData(SemanticCaptureConditionKind.Removed, "EmptyCaptureKey")]
    public void should_compile_guarded_field_literal_optional_and_tagged_mappings(SemanticCaptureConditionKind kind, string name)
    {
        _model = Transform(_model, slice =>
        {
            if (slice.Captures.IsEmpty) return slice;
            var capture = slice.Captures.Single();
            var append = capture.Appends.Single();
            var target = slice.Events.Single(contract => contract.Id == append.EventContract);
            var literal = target.Properties.Single() with { Id = SemanticId.Create(SemanticKind.Property, "literal"), Name = "label", Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) };
            var optional = literal with { Id = SemanticId.Create(SemanticKind.Property, "optional"), Name = "note", Type = literal.Type with { IsOptional = true } };
            return slice with
            {
                Events = [.. slice.Events.Select(contract => contract.Id == target.Id ? contract with { Properties = [.. contract.Properties, literal, optional] } : contract)],
                Captures = [capture with { Name = name, Appends = [append with
                {
                    When = new(kind, kind is SemanticCaptureConditionKind.Removed or SemanticCaptureConditionKind.Added ? [] : ["orderId"]),
                    Tags = ["translated"],
                    Mappings = [.. append.Mappings, append.Mappings.Single() with { TargetProperty = literal.Id, Field = null, Value = new SemanticValueExpression(new SemanticTextValue("fixed")) }]
                }] }]
            };
        });
        Render();
        Assert.Empty(RenderedOutput.Warnings(_sources));
    }

    [Fact]
    public void should_compile_actual_isolated_emitter_overloads_with_stable_semantic_id_mapping()
    {
        Render();
        var mappings = new List<string[]>();
        foreach (var reverse in new[] { false, true })
        {
            var probe = InternalCaptureProbe(sameNamespace: false, reverse);
            var plan = probe.Context.CaptureMembers(probe.Capture);
            Assert.Empty(plan.Refusals);
            Assert.Equal("OnShipmentDispatched_", plan.Handler(probe.First));
            Assert.Equal(plan.Handler(probe.First), plan.Handler(probe.Second));
            var emitted = SemanticCaptureArtifactRenderer.Render(probe.Located, probe.Capture, probe.Context);
            var support = new CodeGeneration.RenderedFile("SecondSource.cs", "namespace Contracts.Integration.Translation.PackOrder; public record ShipmentDispatched(global::Contracts.Common.OrderId OrderId);");
            var files = _sources.Where(file => !file.RelativePath.EndsWith("ShipmentTracking.cs", StringComparison.Ordinal)).Append(support).Append(emitted).ToArray();
            Assert.Empty(RenderedOutput.Errors(files));
            Assert.Empty(RenderedOutput.Warnings(files));
            var assembly = RenderedOutput.Load(files);
            var methods = assembly.GetTypes().Single(type => type.Name == plan.TypeName).GetMethods().Where(method => method.Name == plan.Handler(probe.First)).ToArray();
            Assert.Equal(2, methods.Length);
            Assert.NotEqual(methods[0].GetParameters()[0].ParameterType, methods[1].GetParameters()[0].ParameterType);
            mappings.Add([plan.Handler(probe.First), plan.Handler(probe.Second), .. methods.Select(method => method.GetParameters()[0].ParameterType.FullName!).Order(StringComparer.Ordinal)]);
        }
        Assert.Equal(mappings[0], mappings[1]);
    }
}
#endif
