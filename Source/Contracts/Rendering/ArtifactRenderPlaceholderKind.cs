// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Semantic placeholder vocabulary; syntax and emission remain provider-owned.</summary>
public enum ArtifactRenderPlaceholderKind
{
    /// <summary>An operation body that throws when invoked.</summary>
    ThrowingOperationBody,

    /// <summary>A denied enclosing operation.</summary>
    DenyEnclosingOperation,

    /// <summary>A disabled specification.</summary>
    DisabledSpecification,

    /// <summary>An unregistered consumer.</summary>
    UnregisteredConsumer,

    /// <summary>A retained signature or type.</summary>
    RetainedSignatureOrType,

    /// <summary>A graph-proven omitted leaf, never inferred from its kind.</summary>
    GraphProvenOmittedLeaf
}
