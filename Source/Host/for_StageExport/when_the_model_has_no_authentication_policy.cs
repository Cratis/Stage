// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Publishing;
using Xunit;

namespace Cratis.Stage.Host.Export.for_StageExport;

public class when_the_model_has_no_authentication_policy : given.an_exportable_model
{
    StageExportRefused _refused = null!;

    void Establish() => File.WriteAllText(Path.Combine(_model, "shop.play"), Open);

    async Task Because() => _refused = await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("out"))));

    [Fact] void should_refuse() => _refused.Message.ShouldContain("no authentication policy");
    [Fact] void should_write_nothing() => Directory.Exists(Output("out")).ShouldBeFalse();
}
