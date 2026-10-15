// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Represents one top-level C# type the rendered application declares.
/// </summary>
/// <param name="Artifact">The Screenplay artifact the declaration is rendered from.</param>
/// <param name="Kind">The kind of declaration, such as Command, Event or Generated.</param>
/// <param name="Namespace">The namespace relative to the root namespace; empty for the root namespace itself.</param>
/// <param name="Name">The emitted type name.</param>
internal sealed record GeneratedTypeDeclaration(SemanticId Artifact, string Kind, string Namespace, string Name);
