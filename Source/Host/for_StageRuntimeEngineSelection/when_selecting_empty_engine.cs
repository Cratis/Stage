// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageRuntimeEngineSelection;

public class when_selecting_empty_engine : Specification
{
    StageRuntimeEngine _selected;

    void Because() => _selected = StageRuntimeEngineSelection.Read(["--engine=  "]);

    [Fact] void should_use_the_default_eventmodel_engine() => _selected.ShouldEqual(StageRuntimeEngine.EventModel);
}
