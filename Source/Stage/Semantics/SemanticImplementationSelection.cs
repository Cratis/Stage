// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Semantics;

internal sealed record SemanticImplementationSelection(HashSet<SemanticId> Commands, HashSet<SemanticId> Queries, SemanticBodyExecution? Bodies);
