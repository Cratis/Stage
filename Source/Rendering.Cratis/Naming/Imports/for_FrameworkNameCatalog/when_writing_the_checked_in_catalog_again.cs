// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_writing_the_checked_in_catalog_again : Specification
{
    string _text = null!;
    string _written = null!;

    void Establish()
    {
        using var stream = typeof(FrameworkNameCatalog).Assembly.GetManifestResourceStream(FrameworkNameCatalog.ResourceName)!;
        using var reader = new StreamReader(stream);
        _text = reader.ReadToEnd();
    }

    void Because() => _written = FrameworkNameCatalogText.Write(FrameworkNameCatalogText.Read(_text));

    [Fact] void should_produce_the_same_bytes() => (_written == _text).ShouldBeTrue();
}
