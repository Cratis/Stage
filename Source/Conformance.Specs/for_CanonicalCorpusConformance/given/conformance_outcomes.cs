// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Reduces what a surface did with a vector to one cell: `supported`, `refused:<CODE>[+<CODE>]` or `gap:<what>`.
// Only a typed STAGE code (or a Host capability name) is an acceptable refusal; everything else is a gap.
public static partial class conformance_outcomes
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string Supported = "supported";
    const string PlanRefusal = "STAGE-PLAN-004";
    const string NoSpecifications = "n/a:no-specifications";

    public static async Task<string> Execute(conformance_surface surface, conformance_vector vector)
    {
        try
        {
            var compiled = SemanticExecutionPlan.Compile(vector.Model);
            if (compiled.Plan is not { } plan)
            {
                // The model loader (SemanticModelLoader) refuses a model whose plan does not compile before any surface sees it.
                return $"refused:{PlanRefusal}";
            }

            return surface switch
            {
                conformance_surface.Executor => await Executor(vector, plan),
                conformance_surface.Host => Host(plan),
                _ => Renderer(vector, plan)
            };
        }
        catch (Exception exception)
        {
            return $"gap:threw({exception.GetType().Name})";
        }
    }

    [GeneratedRegex(@"^(STAGE-[A-Z]+-\d+):")]
    private static partial Regex TypedCode();

    static async Task<string> Executor(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var report = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new());
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var result in report.Results)
        {
            var expectation = vector.Corpus?.SpecificationExpectations.FirstOrDefault(value => value.Specification.ToString() == result.SpecificationId);
            switch (result.Outcome)
            {
                case SemanticSpecificationOutcome.Passed when expectation is { Passed: false } or { Outcome: SemanticExecutionOutcomeKind.Unsupported }:
                    problems.Add("passed-against-corpus-expectation");
                    break;
                case SemanticSpecificationOutcome.Passed:
                    break;
                case SemanticSpecificationOutcome.Failed when expectation is { Passed: false }:
                    break;
                case SemanticSpecificationOutcome.Unsupported when result.Unsupported is { } unsupported && TypedCode().Match(unsupported.Details) is { Success: true } match:
                    codes.Add(match.Groups[1].Value);
                    break;
                case SemanticSpecificationOutcome.Unsupported when result.Unsupported is { } unsupported:
                    codes.Add($"capability:{unsupported.Capability}");
                    break;
                case SemanticSpecificationOutcome.Unsupported:
                    problems.Add("untyped-refusal");
                    break;
                default:
                    problems.Add(result.Outcome.ToString().ToLowerInvariant());
                    break;
            }
        }

        return report.Results.Count == 0 && problems.Count == 0 ? NoSpecifications : Classify(null, codes, problems);
    }

    static string Host(SemanticExecutionPlan plan)
    {
        var type = Assembly.Load("Cratis.Stage.Host").GetType("Cratis.Stage.Host.SemanticRuntimeAdmission", true)!;
        var admission = Activator.CreateInstance(type, Instance, null, [plan], null)!;
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in (IEnumerable)type.GetProperty("Entries", Instance)!.GetValue(admission)!)
        {
            string Read(string name) => (string?)entry.GetType().GetProperty(name, Instance)!.GetValue(entry) ?? string.Empty;
            if (Read("Status") == Supported)
            {
                continue;
            }

            var details = Read("Details");
            var capability = Read("Capability");
            if (TypedCode().Match(details) is { Success: true } match)
            {
                codes.Add(match.Groups[1].Value);
            }
            else if (capability.Length > 0)
            {
                // A refusal with a named capability and no STAGE code (an evolved event, an opaque rule); the table records it as such.
                codes.Add($"capability:{capability}");
            }
            else
            {
                problems.Add($"untyped-refusal({Read("Kind")})");
            }
        }

        return Classify(null, codes, problems);
    }

    static string Renderer(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var name = vector.Name.Replace('.', '_').Replace('[', '_').Replace("]", string.Empty, StringComparison.Ordinal);
        var rendered = CratisRendering.Plan(vector.Model, plan, new(ArtifactRenderScopeKind.Application, vector.Model.Application.Id), new(name, name));
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        if (rendered.Success)
        {
            if (rendered.Artifacts.Length == 0)
            {
                problems.Add("rendered-nothing");
            }

            return Classify(null, codes, problems);
        }

        if (rendered.Artifacts.Length > 0)
        {
            problems.Add("blocked-with-artifacts");
        }

        foreach (var diagnostic in rendered.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            if (TypedCode().IsMatch(diagnostic.Code + ":"))
            {
                codes.Add(diagnostic.Code);
            }
            else
            {
                problems.Add($"untyped-refusal({diagnostic.Code})");
            }
        }

        return Classify(codes.Count == 0 && problems.Count == 0 ? "blocked-without-diagnostics" : null, codes, problems);
    }

    static string Classify(string? emptyProblem, SortedSet<string> codes, SortedSet<string> problems)
    {
        if (emptyProblem is not null)
        {
            problems.Add(emptyProblem);
        }

        return problems.Count > 0 ? $"gap:{string.Join("+", problems)}" : codes.Count == 0 ? Supported : $"refused:{string.Join("+", codes)}";
    }
}
#endif
