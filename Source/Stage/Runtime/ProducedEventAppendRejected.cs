// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when Chronicle does not accept a modeled command's produced events.
/// </summary>
/// <param name="reason">The reason Chronicle rejected the append.</param>
public sealed class ProducedEventAppendRejected(string reason) : Exception(reason);
