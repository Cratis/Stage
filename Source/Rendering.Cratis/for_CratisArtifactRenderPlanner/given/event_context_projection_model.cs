// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Plans projections that map eventContext values, as the Axon views sample does for its ContextTotal read model.
/// </summary>
public static class event_context_projection_model
{
    const string Header = """
        type CounterDetails
          note String
        concept MemberId : Uuid
        type ContextNote
          noteId MemberId
          seen DateTime optional
        module Home
          feature Ctx
        """;

    public const string Plain = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              readmodel ContextTotal
                name String key
                occurred DateTime optional
                correlation Uuid optional
                details CounterDetails
              projection ContextProjection => ContextTotal
                from ContextRecorded
                  key "global"
                  name = "global"
                  occurred = $eventContext.occurred
                  correlation = $eventContext.correlationId
                  details.note = "increased"
              query AllContextTotals => ContextTotal[]
        """;

    public const string AxonViews = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              readmodel ContextTotal
                name String key
                occurred DateTime
                correlation Uuid optional
                details CounterDetails optional
              projection ContextProjection => ContextTotal
                from ContextRecorded
                  key "global"
                  name = "global"
                  occurred = $eventContext.occurred
                  correlation = $eventContext.correlationId
                  details.note = "increased"
              query AllContextTotals => ContextTotal[]
        """;

    public const string NestedThroughOptional = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              readmodel ContextTotal
                name String key
                details CounterDetails optional
              projection ContextProjection => ContextTotal
                from ContextRecorded
                  key "global"
                  name = "global"
                  details.note = "increased"
              query AllContextTotals => ContextTotal[]
        """;

    public const string NestedThroughOptionalReadByACommand = """
        command reads protected
        type CounterDetails
          note String
        module Home
          feature Ctx
            slice StateView Totals
              event Counted
                label String
              event Uncounted
              readmodel Total
                id String key
                label String
                details CounterDetails optional
              projection TotalProjection => Total
                from Counted
                  id = $eventSourceId
                  label = label
                  details.note = "counted"
                remove with Uncounted
              query ById => Total optional
                by id String
            slice StateChange Recording
              event Recorded
              command Record
                id String identifier
                reads Total by id
                validate
                  require Total.label == "open"
                    message "Only an open total may be recorded."
                produces Recorded
                  for id
        """;

    public const string ReadByACommand = """
        command reads protected
        concept MemberId : Uuid
        module Home
          feature Ctx
            slice StateView Totals
              event Counted
                label String
              event Uncounted
              readmodel Total
                id String key
                label String
                seen DateTime optional
              projection TotalProjection => Total
                from Counted
                  id = $eventSourceId
                  label = label
                  seen = $eventContext.occurred
                remove with Uncounted
              query ById => Total optional
                by id String
            slice StateChange Recording
              event Recorded
              command Record
                id String identifier
                reads Total by id
                validate
                  require Total.label == "open"
                    message "Only an open total may be recorded."
                produces Recorded
                  for id
        """;

    public const string KeyedByContext = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                correlation Uuid
              readmodel ContextTotal
                correlation Uuid key
              projection ContextProjection => ContextTotal
                from ContextRecorded key correlation
                  correlation = $eventContext.correlationId
              query ByCorrelation => ContextTotal optional
                by correlation Uuid
        """;

    public const string FilteredByContext = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              readmodel ContextTotal
                name String key
                occurred DateTime
              projection ContextProjection => ContextTotal
                from ContextRecorded key name
                  name = name
                  occurred = $eventContext.occurred
              query ByName => ContextTotal?
                by name String
              query Since => ContextTotal[]
                filter occurred DateTime
        """;

    public const string InAJoin = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              event ContextNamed
                label String
              readmodel ContextTotal
                name String key
                label String
                seen DateTime
              projection ContextProjection => ContextTotal
                from ContextRecorded key name
                  name = name
                join context on name
                  with ContextNamed
                    label = label
                    seen = $eventContext.occurred
              query AllContextTotals => ContextTotal[]
        """;

    public const string InAChild = Header + """

            slice StateView ContextTotals
              event ContextRecorded
                name String
              event ContextNoted
                noteId MemberId
                name String
              readmodel ContextTotal
                name String key
                notes ContextNote[]
              projection ContextProjection => ContextTotal
                from ContextRecorded key name
                  name = name
                children notes identified by noteId
                  from ContextNoted
                    noteId = noteId
                    seen = $eventContext.occurred
              query AllContextTotals => ContextTotal[]
        """;

    public static ArtifactRenderPlan Plan(string source, bool strict = false)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ContextModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("context"), "context", "Context.play", source);
        var compilation = new SemanticModelCompiler().Compile("ContextModel", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics.Select(_ => $"{_.Code} {_.Severity}: {_.Message}")));
        }

        var model = compilation.Value!.Model;
        var options = new CratisRenderingOptions("ContextApp", "Context") { Strict = strict };

        // Screenplay leaves the unexecutable event-context mappings out of the execution plan; the renderer decides what to do with them.
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model, []).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            IdentityCatalog = compilation.Value.Documents.IdentityCatalog,
            SemanticIdentities = compilation.Value.Documents.IdentityCatalog.Semantics
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }
}
