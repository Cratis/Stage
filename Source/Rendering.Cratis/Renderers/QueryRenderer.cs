// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders the queries a slice declares as the static query methods of its read model.
/// </summary>
/// <remarks>
/// A read model used to receive a fixed <c>All&lt;Plural&gt;</c> and <c>&lt;Type&gt;ById</c> pair whatever the
/// document said. A document declaring <c>GetOverdueInvoices</c> got neither that method nor a mention of it, and
/// got two methods nobody had written — so the names in the rendered application answered to nothing in the
/// document, and a caller reading the document could not find them.
/// <para>
/// The fixed pair is still what a read model with no declared query receives. Something has to be able to read it,
/// and inventing a way in is the lesser of the two only while the document states none.
/// </para>
/// <para>
/// A query the document declares <c>observable</c> renders as a live one — an <c>ISubject</c> fed by the
/// collection's change stream. It used to render as its one-shot counterpart and say nothing about it, so a
/// document asking for a query that keeps pushing produced one that answered once and looked correct.
/// </para>
/// <para>
/// A query names the read model it reads with its return type, and only the queries naming <i>this</i> read model
/// are rendered onto it. Every declared query used to be rendered against the slice's first projection whatever
/// its return type said, so a query returning <c>OverdueInvoices</c> came out reading <c>InvoiceSummary</c> —
/// a different read model, substituted in silence. What a read model is not the return type of is reported by
/// <see cref="UnrenderedConstructs"/> instead of rendered against the wrong one.
/// </para>
/// <para>
/// Authorization belongs to each generated method. Combining query policies on the read-model type changes two
/// distinct operations into one wider surface: an administrator-only <c>All</c> and auditor-only <c>Mine</c>
/// would both admit either role. Rendering the attribute immediately before its method preserves the operation
/// boundary Arc evaluates.
/// </para>
/// </remarks>
public static class QueryRenderer
{
    /// <summary>
    /// The namespace holding <c>ISubject&lt;T&gt;</c>, the return type of a live query. The scaffolded project does
    /// not make it ambient, so a file rendering an observable query imports it.
    /// </summary>
    public const string ObservableNamespace = "System.Reactive.Subjects";

    /// <summary>
    /// Renders the query methods for a read model.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to render into.</param>
    /// <param name="typeName">The rendered read model type name.</param>
    /// <param name="keyType">The type of the read model's key.</param>
    /// <param name="keyParameterName">The name the key is rendered under.</param>
    /// <param name="queries">The queries the slice declares, across all of its read models.</param>
    /// <param name="applicationSet">The application set the query's authorization policies resolve against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <exception cref="AuthorizationCannotBeRendered">A generated query method's authorization cannot be represented faithfully.</exception>
    public static void Render(
        CSharpCodeBuilder builder,
        string typeName,
        string keyType,
        string keyParameterName,
        IEnumerable<QuerySyntax> queries,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var own = queries.Where(query => Reads(query, typeName)).ToArray();

        builder.BlankLine();

        if (own.Length == 0)
        {
            RenderTheFixedPair(builder, typeName, keyType, keyParameterName);
            return;
        }

        if (own.Any(query => query.IsObservable))
        {
            builder.Using(ObservableNamespace);
        }

        foreach (var query in own)
        {
            var authorization = AuthorizationRenderer.Render(
                query.Authorize,
                applicationSet,
                $"Query '{query.Name}'",
                diagnostics);
            builder.Attribute(authorization).Line(Signature(query, typeName, keyType, keyParameterName));
        }
    }

    /// <summary>
    /// Whether a query reads the given read model. The return type names it, whether the query answers with one
    /// instance or a collection of them.
    /// </summary>
    /// <param name="query">The <see cref="QuerySyntax"/> to attribute.</param>
    /// <param name="readModel">The rendered read model's C# type name.</param>
    /// <returns>True when the query reads that read model.</returns>
    /// <remarks>
    /// The one place the attribution is decided, so a query method and its authorization are rendered on the
    /// read model the query actually returns.
    /// </remarks>
    public static bool Reads(QuerySyntax query, string readModel) =>
        Identifiers.ToPascalCase(query.ReturnType.Name).Equals(readModel, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether a query is rendered as a method at all, so what is not can be reported.
    /// </summary>
    /// <param name="query">The query to consider.</param>
    /// <param name="readModel">The read model rendered in the file, or <see langword="null"/> when none is.</param>
    /// <returns>True when a method is rendered for it.</returns>
    /// <remarks>
    /// A method for a query lives on the read model the query returns, so a slice rendering no read model — or one
    /// the query does not name — renders no method for it. Rendering it against the read model that <i>is</i> there
    /// would answer with a different read model than the document states, which is an invented value rather than a
    /// missing one.
    /// </remarks>
    public static bool HasRenderedMethod(QuerySyntax query, string? readModel) =>
        readModel is not null && Reads(query, readModel);

    /// <summary>
    /// Determines whether a query's rendered method answers everything the document states, so what does not can
    /// be reported.
    /// </summary>
    /// <param name="query">The query to consider.</param>
    /// <returns>True when the rendered method answers what the document states.</returns>
    /// <remarks>
    /// A query whose logic lives in a performer is not rendered — the document delegates the body to a file or an
    /// inline block, and neither is read here. Its name is still rendered, so the application answers to the
    /// document; what is missing is what it does, which is what gets reported.
    /// </remarks>
    public static bool IsFullyRendered(QuerySyntax query) => query.Performer is null && !query.Filters.Any();

    // Named after the query the document declares. A collection reads the whole model; a query naming an
    // identifying parameter with 'by' reads one instance through it; anything else answers the model itself.
    static string Signature(QuerySyntax query, string typeName, string keyType, string keyParameterName)
    {
        var name = Identifiers.ToPascalCase(query.Name);
        var parameter = query.By is { } by ? Identifiers.ToCamelCase(by.Name) : keyParameterName;
        var readsTheWholeModel = query.By is null && query.ReturnType.IsCollection;

        if (query.IsObservable)
        {
            return Live(name, typeName, keyType, parameter, readsTheWholeModel);
        }

        return readsTheWholeModel
            ? $"public static IQueryable<{typeName}> {name}(IMongoCollection<{typeName}> collection) => collection.AsQueryable();"
            : $"public static async Task<{typeName}?> {name}(IReadModels readModels, {keyType} {parameter}) => " +
              $"await readModels.GetInstanceById<{typeName}>((EventSourceId){parameter});";
    }

    // A query declared 'observable' reads what its non-observable counterpart reads, and keeps reading it: the
    // ISubject the Cratis.Arc.MongoDB extensions on IMongoCollection<T> hand back is fed by the collection's
    // change stream. It is returned directly and never wrapped in a Task — a live query that has to be awaited
    // before it can be subscribed to answers once, which is the very thing the document asked it not to do.
    static string Live(string name, string typeName, string keyType, string parameter, bool readsTheWholeModel) =>
        readsTheWholeModel
            ? $"public static ISubject<IEnumerable<{typeName}>> {name}(IMongoCollection<{typeName}> collection) => collection.Observe();"
            : $"public static ISubject<{typeName}> {name}(IMongoCollection<{typeName}> collection, {keyType} {parameter}) => " +
              $"collection.ObserveById({parameter});";

    static void RenderTheFixedPair(CSharpCodeBuilder builder, string typeName, string keyType, string keyParameterName) =>
        builder
            .Line($"public static IQueryable<{typeName}> All{Pluralizer.Pluralize(typeName)}(IMongoCollection<{typeName}> collection) => collection.AsQueryable();")
            .Line($"public static async Task<{typeName}?> {typeName}ById(IReadModels readModels, {keyType} {keyParameterName}) => " +
                  $"await readModels.GetInstanceById<{typeName}>((EventSourceId){keyParameterName});");
}
