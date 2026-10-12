// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;

namespace Cratis.Stage.Semantics.for_SemanticSchemas.given;

public class a_schema : Specification
{
    protected static string Schema(string declarations, string type)
    {
        var source = $"{declarations}\nmodule Records\n  feature Registration\n    slice StateChange Register\n      event Registered\n        value {type}\n";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("records"), "records", "Records.play", source);
        var compilation = new SemanticModelCompiler().Compile("Records", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var application = compilation.Value!.Model.Application;
        var properties = application.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties;

        return SemanticSchemas.Schema(properties, application);
    }

    protected static JsonObject Value(string schema) => JsonNode.Parse(schema)!["properties"]!["value"]!.AsObject();
}
