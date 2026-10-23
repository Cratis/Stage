// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Base for the generated event property types that carry a composite or collection value as its authored JSON.
/// Each generated subclass names a schema-only shape through Chronicle's JsonSchemaType attribute, so
/// Chronicle reads exact Decimal and large whole-number members while the written bytes stay as authored.
/// </summary>
public abstract class SemanticStructuredEventValue
{
    /// <summary>
    /// Gets or sets the authored JSON value, written and read back verbatim.
    /// </summary>
    public JsonElement Value { get; set; }
}
