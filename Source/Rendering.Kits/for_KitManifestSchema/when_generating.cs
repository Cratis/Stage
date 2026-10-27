// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitManifestSchema;

public class when_generating : Specification
{
    string _generated = string.Empty;
    string _checkedIn = string.Empty;

    void Establish()
    {
        using var stream = typeof(KitManifestSchema).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Kits.stage-target.v1.schema.json")!;
        using var reader = new StreamReader(stream);
        _checkedIn = reader.ReadToEnd();
    }

    void Because() => _generated = KitManifestSchema.Generate();

    [Fact] void should_match_the_checked_in_schema() => _generated.ShouldEqual(_checkedIn);
}
