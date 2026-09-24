// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageRuntimeEngineSelection;

public class when_selecting_explicit_eventmodel : Specification
{
    StageRuntimeEngine _selected;

    void Because() => _selected = StageRuntimeEngineSelection.Read(["--engine=eventmodel"]);

    [Fact] void should_keep_the_original_engine() => _selected.ShouldEqual(StageRuntimeEngine.EventModel);
}
