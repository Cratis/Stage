// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts.for_ReadModelConverter.given;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_SliceConverter.when_converting_with_diagnostics;

public class and_the_projection_is_unsupported : a_slice_with_projections
{
    Slice _result = null!;

    void Establish()
    {
        const string source = """
            module Store
              feature Registration
                slice StateChange RegisterItem
                  command RegisterItem
                    name String
                    produces ItemRegistered
                      name = name
                  event ItemRegistered
                    name String
                  specification RegistersAnItem
                    when RegisterItem
                      name = "One"
                    then ItemRegistered
                      name = "One"
            """;
        var compiled = new ScreenplayCompiler().Compile(source);
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        var slice = compiled.Value!.Modules.Single().Features.Single().Slices.Single();
        _slice = slice with { Projections = _slice.Projections };
    }

    void Because() => _result = SliceConverter.Convert(_slice, _schema, new Dictionary<string, string>(), "Store.Registration", _diagnostics);

    [Fact] void should_keep_the_command() => _result.Command!.Name.ShouldEqual("RegisterItem");
    [Fact] void should_keep_the_events() => _result.Events.Single().Name.ShouldEqual("ItemRegistered");
    [Fact] void should_keep_the_specifications() => _result.Specifications.Count.ShouldEqual(1);
    [Fact] void should_keep_the_read_model_without_its_projection() => _result.ReadModel!.UnconvertedProjectionName.ShouldEqual("ItemsProjection");
    [Fact] void should_report_the_omission() => _diagnostics.All[0].Path.ShouldEqual("Store.Registration.RegisterItem");
}
