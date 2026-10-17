// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

internal interface ISemanticFactReader
{
    Task<ImmutableArray<SemanticFact>> Read(ulong from, ulong through);
}
