// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_routed_model : Specification
{
    string _directory = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage routed model {Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData("eventsource Invoice\n  stream Changes\n")]
    [InlineData("eventsource Invoice\n")]
    [InlineData("example Registered : InvoiceRegistered\n  no stream\n")]
    public async Task should_refuse_before_translating_the_event_model_or_scene(string declaration)
    {
        var source = declaration + "module Billing\n  feature Invoices\n    slice StateChange Register\n      event InvoiceRegistered\n";
        var fromSource = Catch.Exception(() => EventModelLoader.LoadFromSource(source));
        fromSource.ShouldBeOfExactType<UnsupportedEventSourceRoutes>();
        fromSource.Message.StartsWith("STAGE-ESM-030:", StringComparison.Ordinal).ShouldBeTrue();
        var path = Path.Combine(_directory, "routed.play");
        await File.WriteAllTextAsync(path, source);
        foreach (var input in new[] { path, _directory })
        {
            var fromPath = await Catch.Exception(() => EventModelLoader.LoadStageApplicationFromPathAsync(input));
            fromPath.ShouldBeOfExactType<UnsupportedEventSourceRoutes>();
            ((UnsupportedEventSourceRoutes)fromPath).Location.Line.ShouldEqual(1);
        }
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
