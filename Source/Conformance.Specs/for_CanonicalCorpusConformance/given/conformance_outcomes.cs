// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Reduces what a surface did with a vector to one cell: `supported`, `refused:<what>[+<what>]`, `n/a:<why>` or `gap:<what>`.
// <what> is `<code>@<construct>`: a typed STAGE code, or `capability:<Name>` when only the capability types the refusal, followed by the
// construct that was refused, so a refusal that moves to another construct changes the cell. Anything untyped is a gap.
public static partial class conformance_outcomes
{
    const string Supported = "supported";
    const string NoSpecifications = "n/a:no-specifications";
    const string NotLoaded = "n/a:not-loaded";
    const string ExpectedRefusal = "expected-refusal:";
    const string LoaderRefusal = "STAGE-PLAN-004";

    public static async Task<string> Execute(conformance_surface surface, conformance_vector vector)
    {
        try
        {
            if (surface == conformance_surface.Loader)
            {
                return Loader(vector);
            }

            // The model loader refuses a plan with issues, but the surfaces still run on the partial plan, as the golden-vector specs admit it.
            if (SemanticExecutionPlan.Compile(vector.Model, []).Plan is not { } plan)
            {
                return NotLoaded;
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
            return $"gap:threw({exception.GetType().Name}: {exception.Message})";
        }
    }

    [GeneratedRegex(@"^(STAGE-[A-Z]+-\d+):", RegexOptions.None, 1000)]
    private static partial Regex TypedCode { get; }

    static string Loader(conformance_vector vector)
    {
        var compiled = SemanticExecutionPlan.Compile(vector.Model);
        var issues = compiled.Issues.ToArray();
        if (compiled.Plan is not null && issues.Length == 0)
        {
            return Supported;
        }

        var kinds = new SortedSet<string>(issues.Select(issue => issue.Kind.ToString()), StringComparer.Ordinal);
        return kinds.Count == 0 ? "gap:loader-refused-without-issues" : $"refused:{LoaderRefusal}({string.Join("+", kinds)})";
    }

    static async Task<string> Executor(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var report = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new());
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var expectedRefusals = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        if (plan.Specifications.Count == 0)
        {
            return report.Results.Count == 0 ? NoSpecifications : "gap:results-without-specifications";
        }

        problems.UnionWith(executor_result_ids.Problems(
            plan.Specifications.Keys.Select(id => id.ToString()), report.Results.Select(result => result.SpecificationId)));

        // Expectations are matched by id; one that matches no result would otherwise be skipped silently.
        var expectations = vector.Corpus?.SpecificationExpectations ?? [];
        var resultIds = report.Results.Select(result => result.SpecificationId).ToHashSet(StringComparer.Ordinal);
        if (expectations.Any(expectation => !resultIds.Contains(expectation.Specification.ToString())))
        {
            problems.Add("expectation-id-matches-no-result");
        }

        foreach (var result in report.Results)
        {
            var expectation = expectations.FirstOrDefault(value => value.Specification.ToString() == result.SpecificationId);
            switch (result.Outcome)
            {
                case SemanticSpecificationOutcome.Passed when expectation is { Passed: false } or { Outcome: SemanticExecutionOutcomeKind.Unsupported }:
                    problems.Add("passed-against-corpus-expectation");
                    break;
                case SemanticSpecificationOutcome.Passed:
                    break;
                case SemanticSpecificationOutcome.Failed when expectation is { Passed: false }:
                    break;
                case SemanticSpecificationOutcome.Unsupported when result.Unsupported is { } expected && expectation is { Outcome: SemanticExecutionOutcomeKind.Unsupported }:
                    // The corpus itself says this specification is unsupported, so the refusal is the expected result, not a gap to track.
                    expectedRefusals.Add($"{CodeOrCapability(expected.Details, expected.Capability.ToString())}@{Short(expected.Construct)}");
                    break;
                case SemanticSpecificationOutcome.Unsupported when result.Unsupported is { } unsupported:
                    codes.Add($"{CodeOrCapability(unsupported.Details, unsupported.Capability.ToString())}@{Short(unsupported.Construct)}");
                    break;
                case SemanticSpecificationOutcome.Unsupported:
                    problems.Add("untyped-refusal");
                    break;
                default:
                    problems.Add(result.Outcome.ToString().ToLowerInvariant());
                    break;
            }
        }

        return codes.Count == 0 && problems.Count == 0 && expectedRefusals.Count > 0
            ? $"{ExpectedRefusal}{string.Join("+", expectedRefusals)}"
            : Classify(codes, problems);
    }

    static string Host(SemanticExecutionPlan plan)
    {
        var refused = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in new SemanticRuntimeAdmission(plan).Entries.Where(entry => entry.Status != Supported))
        {
            var details = entry.Details ?? string.Empty;
            var capability = entry.Capability ?? string.Empty;
            if (TypedCode.IsMatch(details) || capability.Length > 0)
            {
                // Recorded per construct kind, so a refusal that moves to another kind changes the cell.
                refused.Add($"{CodeOrCapability(details, capability)}@{entry.Kind}");
            }
            else
            {
                problems.Add($"untyped-refusal({entry.Kind})");
            }
        }

        return Classify(refused, problems);
    }

    static string Renderer(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var name = vector.Name.Replace('.', '_').Replace('[', '_').Replace("]", string.Empty, StringComparison.Ordinal);

        // The scaffold takes the application name as a C# identifier; a golden is named in prose ("Canonical Golden Application"), which the
        // scaffold rejects before it looks at a single construct. Rename only the application, so the planner sees the vector's constructs.
        var model = vector.Model;
        if (!IdentifierName.IsMatch(model.Application.Name))
        {
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Name = name });
            plan = SemanticExecutionPlan.Compile(model, []).Plan!;
        }

        var source = conformance_source_bodies.Compile(vector);
        if (source.Problem is not null)
        {
            return $"gap:{source.Problem}";
        }

        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);

        // A vector with source forms is planned from its compiled source with the implementation requirements, bodies and typed contexts,
        // as the three-way specs plan it. Without them every inline body would be refused as a missing envelope whatever Stage can do.
        var rendered = source.Loaded is { } loaded
            ? CratisRendering.Plan(loaded.Model, loaded.Plan, scope, new(name, name), loaded.ImplementationRequirements, loaded.ImplementationContents, loaded.AttachmentDiagnostics, loaded.TypedContextDescriptors)
            : CratisRendering.Plan(model, plan, scope, new(name, name));
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        if (rendered.Success)
        {
            if (rendered.Artifacts.Length == 0)
            {
                problems.Add("rendered-nothing");
            }

            return Classify(codes, problems);
        }

        if (rendered.Artifacts.Length > 0)
        {
            problems.Add("blocked-with-artifacts");
        }

        foreach (var diagnostic in rendered.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            if (TypedCode.IsMatch(diagnostic.Code + ":"))
            {
                codes.Add(new renderer_refusal(diagnostic.Code, diagnostic.Artifact.ToString()).ToString());
            }
            else
            {
                problems.Add($"untyped-refusal({diagnostic.Code})");
            }
        }

        if (codes.Count == 0 && problems.Count == 0)
        {
            problems.Add("blocked-without-diagnostics");
        }

        return Classify(codes, problems);
    }

    // Construct ids are long content hashes; the last eight characters tell constructs apart in a human-diffable table.
    static string Short(string? construct) => construct is null ? "none" : construct.Length > 8 ? construct[^8..] : construct;

    // A refusal typed by a STAGE code is that code. One typed only by its capability also carries a short reason key (the first words of
    // its details), so a different reason under the same capability changes the cell.
    static string CodeOrCapability(string details, string capability) =>
        TypedCode.Match(details) is { Success: true } match ? match.Groups[1].Value : $"capability:{capability}[{ReasonKey(details)}]";

    static string ReasonKey(string details) =>
        string.Join('-', NonWord.Replace(details, " ").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5)).ToLowerInvariant();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.None, 1000)]
    private static partial Regex IdentifierName { get; }

    [GeneratedRegex(@"[^A-Za-z0-9]+", RegexOptions.None, 1000)]
    private static partial Regex NonWord { get; }

    static string Classify(SortedSet<string> codes, SortedSet<string> problems) =>
        problems.Count > 0 ? $"gap:{string.Join("+", problems)}" : codes.Count == 0 ? Supported : $"refused:{string.Join("+", codes)}";
}
#endif
