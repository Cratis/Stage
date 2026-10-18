// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

// All fallible semantic-to-CLR and route conversions are completed before an atomic unit starts appending.
internal sealed record SemanticPreparedFact(
    SemanticSpecificationEvent Fact,
    SemanticValue Destination,
    object Instance,
    EventSourceId Source,
    EventStreamType StreamType,
    EventStreamId StreamId,
    EventSourceType SourceType,
    DateTimeOffset? Occurred);
