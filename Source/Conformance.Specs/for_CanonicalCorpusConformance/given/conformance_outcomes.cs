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

// Observes each surface without reducing full artifact identities to cell-level code or kind sets.
public static partial class conformance_outcomes
{
    const string Supported = "supported";
    const string NoSpecifications = "n/a:no-specifications";
    const string NotLoaded = "n/a:not-loaded";
    const string ExpectedRefusal = "expected-refusal";

    public static async Task<conformance_observation> Execute(conformance_surface surface, conformance_vector vector)
    {
        try
        {
            if (surface == conformance_surface.Loader)
            {
                return Loader(vector);
            }

            // Match the loader's partial-plan policy: each surface admits or refuses its own artifacts.
            if (SemanticExecutionPlan.Compile(vector.Model, []).Plan is not { } plan)
            {
                return conformance_observation.WithoutRefusals(NotLoaded);
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
            return conformance_observation.WithoutRefusals($"gap:threw({exception.GetType().Name}: {exception.Message})");
        }
    }

    [GeneratedRegex(@"^(?<code>STAGE-[A-Z]+-\d+):", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TypedCode { get; }

    static conformance_observation Loader(conformance_vector vector)
    {
        var compiled = SemanticExecutionPlan.Compile(vector.Model, []);
        var issues = compiled.Issues.ToArray();
        if (compiled.Plan is not null && issues.Length == 0)
        {
            return conformance_observation.WithoutRefusals(Supported);
        }

        return loader_refusals.Observe(issues.Select(issue => (issue.Kind.ToString(), issue.Artifact.ToString(), issue.Details)));
    }

    static async Task<conformance_observation> Executor(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var report = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new());
        var codes = new Dictionary<refusal_fingerprint, string>();
        var expectedRefusals = new Dictionary<refusal_fingerprint, string>();
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        if (plan.Specifications.Count == 0)
        {
            return conformance_observation.WithoutRefusals(report.Results.Count == 0 ? NoSpecifications : "gap:results-without-specifications");
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
                    expectedRefusals[new(CodeOrCapability(expected.Details, expected.Capability.ToString()), null, expected.Construct ?? "none")] = expected.Details;
                    break;
                case SemanticSpecificationOutcome.Unsupported when result.Unsupported is { } unsupported:
                    codes[new(CodeOrCapability(unsupported.Details, unsupported.Capability.ToString()), null, unsupported.Construct ?? "none")] = unsupported.Details;
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
            ? new(ExpectedRefusal, [.. expectedRefusals.Keys], expectedRefusals)
            : Classify(codes, problems);
    }

    static conformance_observation Host(SemanticExecutionPlan plan)
    {
        var refused = new Dictionary<refusal_fingerprint, string>();
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in HostAdmission.Entries(plan).Where(entry => entry.Status != Supported))
        {
            var details = entry.Details ?? string.Empty;
            var capability = entry.Capability ?? string.Empty;
            if (TypedCode.IsMatch(details) || capability.Length > 0)
            {
                refused[new(CodeOrCapability(details, capability), entry.Kind, entry.Artifact)] = details;
            }
            else
            {
                problems.Add($"untyped-refusal({entry.Kind})");
            }
        }

        return Classify(refused, problems);
    }

    internal static ArtifactRenderPlan? RenderPlan(conformance_vector vector) =>
        SemanticExecutionPlan.Compile(vector.Model, []).Plan is { } plan ? RenderPlan(vector, plan) : null;

    static ArtifactRenderPlan? RenderPlan(conformance_vector vector, SemanticExecutionPlan plan)
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
            return null;
        }

        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);

        // A vector with source forms is planned from its compiled source with the implementation requirements, bodies and typed contexts,
        // as the three-way specs plan it. Without them every inline body would be refused as a missing envelope whatever Stage can do.
        var profile = CratisRendering.CreateProfile(source.Loaded?.Model.Application.Name ?? model.Application.Name, new(name, name), null, canonical_strings_catalog.Files, "en");
        var request = source.Loaded is { } loaded
            ? new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, scope)
            {
                ImplementationRequirements = loaded.ImplementationRequirements,
                ImplementationContents = loaded.ImplementationContents,
                AttachmentDiagnostics = loaded.AttachmentDiagnostics,
                TypedContextDescriptors = loaded.TypedContextDescriptors
            }
            : new ArtifactRenderRequest(model, plan, profile, scope);
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    internal static void RecordCompilation(string vector, IReadOnlyList<compile_error> errors, Dictionary<refusal_fingerprint, string> codes)
    {
        if (errors.Count > 0)
        {
            var first = errors[0];
            codes[new(first.Code, "compile", first.Path)] = $"{vector}: {first.Path}({first.Line}): {first.Code} {first.Message} (+{errors.Count - 1} more)";
        }
    }

    static conformance_observation Renderer(conformance_vector vector, SemanticExecutionPlan plan)
    {
        var rendered = RenderPlan(vector, plan);
        if (rendered is null)
        {
            return conformance_observation.WithoutRefusals($"gap:{conformance_source_bodies.Compile(vector).Problem}");
        }

        var codes = new Dictionary<refusal_fingerprint, string>();
        var problems = new SortedSet<string>(StringComparer.Ordinal);
        if (rendered.Success)
        {
            RecordSpecificationRefusals(rendered, codes);
            if (rendered.Artifacts.Length > 0)
            {
                RecordCompilation(vector.Name, rendered_compilation.Errors(rendered), codes);
            }

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
                codes[new(diagnostic.Code, null, diagnostic.Artifact.ToString())] = diagnostic.Message;
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

    static void RecordSpecificationRefusals(ArtifactRenderPlan rendered, Dictionary<refusal_fingerprint, string> codes)
    {
        var warnings = rendered.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ToArray();
        foreach (var refusal in rendered.Refusals)
        {
            foreach (var code in refusal.Codes)
            {
                codes[new(code, null, refusal.Artifact.ToString())] =
                    warnings.FirstOrDefault(diagnostic => diagnostic.Code == code && diagnostic.Artifact == refusal.Artifact)?.Message ?? code;
            }
        }
    }

    // A refusal typed by a STAGE code is that code. One typed only by its capability also carries a short reason key (the first words of
    // its details), so a different reason under the same capability changes the cell.
    static string CodeOrCapability(string details, string capability) =>
        TypedCode.Match(details) is { Success: true } match ? match.Groups["code"].Value : $"capability:{capability}[{ReasonKey(details)}]";

    static string ReasonKey(string details) =>
        string.Join('-', NonWord.Replace(details, " ").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5)).ToLowerInvariant();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IdentifierName { get; }

    [GeneratedRegex(@"[^A-Za-z0-9]+", RegexOptions.None, 1000)]
    private static partial Regex NonWord { get; }

    static conformance_observation Classify(Dictionary<refusal_fingerprint, string> codes, SortedSet<string> problems)
    {
        var outcome = codes.Count == 0 ? Supported : "refused";
        if (problems.Count > 0)
        {
            outcome = $"gap:{string.Join('+', problems)}";
        }

        return new(outcome, [.. codes.Keys.OrderBy(key => key.ToString(), StringComparer.Ordinal)], codes);
    }
}
#endif
