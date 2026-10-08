// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_temporal_ownership_claims : Specification
{
    static readonly (string Primitive, string Target, string[] Claims)[] _targets =
    [
        ("Date", "2026-09-07", ["2026-09-07", "07/09/2026", "2026-9-7", ""]),
        ("DateTime", "2026-09-07T12:34:56.1234567Z", ["2026-09-07T12:34:56.1234567Z", "2026-09-07T12:34:56.1234567+00:00", "2026-09-07T14:34:56.1234567+02:00", "2026-09-07T12:34:56Z", ""]),
        ("DateTime", "2026-09-07T12:34:56.1234567+02:00", ["2026-09-07T12:34:56.1234567+02:00", "2026-09-07T10:34:56.1234567Z", ""])
    ];

    // Screenplay string literals do not decode JSON Unicode escapes such as \u002B for an offset's '+'.
    static readonly JsonSerializerOptions _literalOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    SemanticExecutionPlan _plan = null!;
    readonly List<string> _mismatches = [];
    int _vectors;

    void Establish()
    {
        // The in-memory executor admits only scalar command values. Unknown-under-'not' parity
        // is covered by the Int/Decimal target specifications from #230, not an absent composite here.
        var source = new StringBuilder("policy Owns\n  require claim \"owner\" matches subject and claim \"owner\" matches invoiceId\n");
        for (var index = 0; index < _targets.Length; index++)
        {
            source.Append($"concept InvoiceId{index} : {_targets[index].Primitive}\n");
        }
        source.Append("module Billing\n  feature Invoicing\n");
        for (var index = 0; index < _targets.Length; index++)
        {
            var target = _targets[index];
            source.Append($$"""
                    slice StateChange Issue{{index}}
                      command Issue{{index}}
                        invoiceId InvoiceId{{index}} identifier
                        authorize Owns
                        produces Issued{{index}}
                          for invoiceId
                          invoiceId = invoiceId
                      event Issued{{index}}
                        invoiceId InvoiceId{{index}}

                """);
            for (var claim = 0; claim < target.Claims.Length; claim++)
            {
                var then = claim == 0 ? $"then Issued{index}\n          for {JsonSerializer.Serialize(target.Target, _literalOptions)}\n          invoiceId = {JsonSerializer.Serialize(target.Target, _literalOptions)}" : "then denied";
                source.Append($$"""
                          specification Claim{{claim}}
                            given caller
                              authenticated
                              claim "OWNER" = {{JsonSerializer.Serialize(target.Claims[claim], _literalOptions)}}
                            when Issue{{index}}
                              invoiceId = {{JsonSerializer.Serialize(target.Target, _literalOptions)}}
                            {{then}}

                    """);
            }
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TemporalOwnership"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("temporal"), "temporal", "Temporal.play", source.ToString());
        var compiled = new SemanticModelCompiler().Compile("TemporalOwnership", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Location}: {diagnostic.Message}")));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
    }

    async Task Because()
    {
        var specifications = _plan.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).ToArray();
        _vectors = specifications.Length;
        var report = await new SemanticSpecificationExecutor().Run(_plan, new([.. specifications.Select(specification => specification.Id)]), new());
        foreach (var specification in specifications)
        {
            var reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
            var stage = report.Results.Single(result => result.SpecificationId == specification.Id.ToString());
            if (!reference.Passed || stage.Outcome != SemanticSpecificationOutcome.Passed)
            {
                _mismatches.Add($"{specification.Name}: reference {reference.Execution.Kind} {string.Join(',', reference.Failures)}; Stage {stage.Outcome} {stage.Unsupported?.Details} {string.Join(',', stage.Failures)}");
            }
        }
    }

    [Fact] void should_run_every_temporal_vector() => _vectors.ShouldEqual(_targets.Sum(target => target.Claims.Length));
    [Fact] void should_agree_with_the_reference_on_exact_text() => Assert.True(_mismatches.Count == 0, string.Join(Environment.NewLine, _mismatches));
}
#endif
