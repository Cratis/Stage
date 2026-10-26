// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// The fixtures of the Arc round trip: every canonical corpus vector whose renderer outcome is 'supported' in the conformance matrix,
// and the named inline sources that the round-trip specs share.
internal static partial class round_trip_fixtures
{
    public const string RoutedCommand = "Inline.RoutedCommand";
    public const string FilteredReactor = "Inline.FilteredReactor";
    public const string EventSourceSuffix = "Inline.EventSourceSuffix";
    public const string StoredNamesFixture = "Inline.StoredNames";

    public const string RoutedSource = """
        concept AccountId : Uuid
        eventsource Account
          identifier AccountId
          stream Transactions
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                amount Decimal
                stream Account.Transactions
                produces event Deposited
                  amount Decimal = amount
        """;

    const string ApplicationName = "Banking";

    public static IReadOnlyList<string> InlineNames { get; } = [RoutedCommand, FilteredReactor, EventSourceSuffix, StoredNamesFixture];

    public static string StoredNames(string sourceId, string streamId) => RoutedSource
        .Replace("eventsource Account\n", $"eventsource Account\n  id \"{sourceId}\"\n", StringComparison.Ordinal)
        .Replace("  stream Transactions\n", $"  stream Transactions\n    id \"{streamId}\"\n", StringComparison.Ordinal);

    public static string InlineSource(string name) => name switch
    {
        RoutedCommand => RoutedSource,
        FilteredReactor => StoredNames("stored-account", "stored-transactions") + """

            slice Automation Forward
              event Forwarded
                amount Decimal
              reaction Follow
                from Account.Transactions
                when Deposited
                  produces Forwarded
                    amount = amount
        """,
        EventSourceSuffix => RoutedSource.Replace("Account", "AccountEventSource", StringComparison.Ordinal),
        StoredNamesFixture => StoredNames("stored-account", "stored-transactions"),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Not a named inline round-trip fixture.")
    };

    public static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("roundtrip"), "roundtrip", "RoundTrip.play", source);
        var compilation = new SemanticModelCompiler().Compile(ApplicationName, SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return compilation.Value!.Model;
    }

    public static round_trip_fixture Inline(string name)
    {
        var model = Compile(InlineSource(name));
        return new(name, model, CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new(ApplicationName, ApplicationName)));
    }

    // The names of every fixture that is eligible for the round trip, before the ledger's exclusions.
    public static IReadOnlyList<string> EligibleNames(expected_conformance_table table) =>
    [
        .. canonical_corpus_vectors.All.Where(vector => IsRendererSupported(table, vector.Name)).Select(vector => vector.Name),
        .. InlineNames
    ];

    public static IEnumerable<round_trip_fixture> Fixtures(expected_conformance_table table, IEnumerable<string> excluded)
    {
        var skipped = excluded.ToHashSet(StringComparer.Ordinal);
        foreach (var vector in canonical_corpus_vectors.All.Where(vector => IsRendererSupported(table, vector.Name) && !skipped.Contains(vector.Name)))
        {
            yield return Corpus(vector);
        }

        foreach (var name in InlineNames.Where(name => !skipped.Contains(name)))
        {
            yield return Inline(name);
        }
    }

    static bool IsRendererSupported(expected_conformance_table table, string vector) =>
        table.Vectors.TryGetValue(vector, out var cells) && cells.TryGetValue(conformance_surface.Renderer, out var cell) && cell.Outcome == "supported";

    // Plans the vector the way the conformance matrix's renderer surface does, so both agree on what 'supported' rendered.
    static round_trip_fixture Corpus(conformance_vector vector)
    {
        var name = vector.Name.Replace('.', '_').Replace('[', '_').Replace("]", string.Empty, StringComparison.Ordinal);
        var model = vector.Model;
        var plan = SemanticExecutionPlan.Compile(model, []).Plan!;
        if (!IdentifierName.IsMatch(model.Application.Name))
        {
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Name = name });
            plan = SemanticExecutionPlan.Compile(model, []).Plan!;
        }

        var source = conformance_source_bodies.Compile(vector);
        Assert.Null(source.Problem);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);
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

        return new(vector.Name, source.Loaded?.Model ?? model, new CratisArtifactRenderPlanner().Plan(request));
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IdentifierName { get; }
}
#endif
