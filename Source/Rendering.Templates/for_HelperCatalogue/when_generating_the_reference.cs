// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_HelperCatalogue;

public class when_generating_the_reference : Specification
{
    [Fact]
    public void should_match_the_checked_in_reference()
    {
        using var reader = new StreamReader(typeof(HelperCatalogue).Assembly.GetManifestResourceStream("template-helpers.md") ?? throw new InvalidOperationException("Missing helper reference."));
        var expected = reader.ReadToEnd();
        var generated = HelperCatalogue.GenerateReference();
        var path = Path.Combine(Path.GetTempPath(), "stage-template-helpers.md");
        if (expected != generated) File.WriteAllText(path, generated);
        Assert.True(expected == generated, "The helper reference differs. Generated text: " + path);
    }
}
