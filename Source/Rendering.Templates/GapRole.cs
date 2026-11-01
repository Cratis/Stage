// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>The safe position of a recoverable placeholder.</summary>
public enum GapRole
{
    /// <summary>A body that fails loudly when invoked.</summary>
    ThrowingOperationBody,

    /// <summary>An enclosing operation that must be denied.</summary>
    DenyEnclosingOperation,

    /// <summary>A specification that must not execute.</summary>
    DisabledSpecification,

    /// <summary>A consumer that must not be registered.</summary>
    UnregisteredConsumer,

    /// <summary>A retained signature or type.</summary>
    RetainedSignatureOrType,

    /// <summary>An omitted leaf proven safe by the graph.</summary>
    GraphProvenOmittedLeaf
}
