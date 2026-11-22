// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticSpecificationEventSources
{
    internal const string Placeholder = "when-append";

    internal static bool RequiresSource(SemanticApplicationContext context, SemanticId contract, bool routed = false) =>
        SemanticSourcelessGivens.RequiresSource(context.Application, contract, routed);

    internal static string Expression(SemanticApplicationContext context, SemanticTypeSystem types, SemanticSpecificationEvent fact)
    {
        if (fact.EventSource is { } source)
        {
            return types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
        }
        if (RequiresSource(context, fact.EventContract, fact.Route is not null))
        {
            throw new InvalidOperationException("An observable event source must be supplied before rendering a specification.");
        }

        // A transport fixture only, never an authored identity or a guessed payload property.
        return CSharpCodeBuilder.StringLiteral(Placeholder);
    }
}
