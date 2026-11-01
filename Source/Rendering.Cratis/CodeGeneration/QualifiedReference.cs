// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Represents a <c language="csharp">global::</c>-qualified type a generated file names.
/// </summary>
/// <param name="Type">The type.</param>
/// <param name="Span">The text its spelling replaces, from <c language="csharp">global::</c> to the end of the type's identifier.</param>
internal sealed record QualifiedReference(TypeReference Type, TextSpan Span);
