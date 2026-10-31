// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>The worker executable or a CLI executable with its hidden worker verb.</summary>
/// <param name="FileName">The executable to start.</param>
/// <param name="Arguments">The arguments passed to the executable.</param>
public sealed record BodyWorkerLaunch(string FileName, ImmutableArray<string> Arguments);
