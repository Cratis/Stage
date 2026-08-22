// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Concepts;

/// <summary>
/// Represents an attribute declared on a concept, such as <c>@pii</c> or <c>@sensitive</c>, together with the
/// reason declared for it.
/// </summary>
/// <param name="Name">The name of the attribute, without the <c>@</c> prefix.</param>
/// <param name="Reason">The reason declared with it, or an empty string when none was declared.</param>
/// <remarks>
/// The marker says a value is personal or sensitive data; the reason says why - the purpose and the lawful
/// basis. A compliance reader needs both, so the reason travels with the marker rather than being lost
/// between the document and whatever consumes this.
/// </remarks>
public record ConceptAttribute(string Name, string Reason);

/// <summary>
/// Represents a <c>concept</c> declared by the application - a strongly typed domain value the events, commands
/// and read models are written in terms of.
/// </summary>
/// <param name="Id">The unique identifier of the concept.</param>
/// <param name="Name">The name of the concept.</param>
/// <param name="Type">The primitive type the concept is based on, or <c>Enum</c> for an enumeration.</param>
/// <param name="IsEnum">Whether the concept is an enumeration rather than a primitive.</param>
/// <param name="Values">The values of the concept when it is an enumeration; empty otherwise.</param>
/// <param name="Attributes">The attributes declared on the concept, such as <c>@pii</c> and <c>@sensitive</c>.</param>
/// <remarks>
/// A concept is declared once for the whole application, which is why it is held on <see cref="EventModel"/>
/// rather than on a slice. It also appears inside a synthesized schema, where a property typed as a concept
/// carries the concept's name and attributes as schema keywords - but a schema only names the concepts some
/// property happens to use, so a concept declared and not yet used had nowhere to live before this.
/// </remarks>
public record ConceptDefinition(
    Guid Id,
    string Name,
    string Type,
    bool IsEnum,
    IReadOnlyList<string> Values,
    IReadOnlyList<ConceptAttribute> Attributes);
