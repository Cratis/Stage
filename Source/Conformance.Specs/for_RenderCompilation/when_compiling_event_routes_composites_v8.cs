// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_compiling_event_routes_composites_v8 : a_captured_render
{
    protected override string FixtureName => "EventRoutesCorpus.CompositesV8";

    async Task Because() => _result = await CompileCaptured(Capture.Plan!);

    [Fact] void should_compile_the_exact_solution_including_generated_specifications() => AssertCompilation();
}
#endif
