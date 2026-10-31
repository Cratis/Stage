// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Host;

/// <summary>The execution plan a world is replayed under, with the out-of-process body execution reducer transitions replay through, if any.</summary>
/// <param name="Plan">The execution plan.</param>
/// <param name="Bodies">The body execution reducer transitions replay through, if any.</param>
internal sealed record SemanticReplayPlan(SemanticExecutionPlan Plan, SemanticBodyExecution? Bodies = null);
