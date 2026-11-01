// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_HelperCatalogue;

public class when_computing_the_surface_digest : Specification
{
    [Fact]
    public void should_pin_the_versioned_helper_signatures()
    {
        var lines = HelperCatalogue.All.Select(helper => helper.Name + "|" + helper.Signature + "|" + helper.Since).Order(StringComparer.Ordinal);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
        digest.ShouldEqual("c999a5c0e67273cf03ebf03d9fd86a7d607f45f9457692dd4d4f4e5b6e387a5a");
    }

    [Fact]
    public void should_expose_frozen_examples_for_every_helper()
    {
        HelperCatalogue.All.Length.ShouldEqual(24);
        HelperCatalogue.All.All(helper => helper.Since == "v1" && !string.IsNullOrWhiteSpace(helper.Summary) && !helper.Vectors.IsDefaultOrEmpty).ShouldBeTrue();
        HelperCatalogue.All.SelectMany(helper => helper.Vectors).Select(vector => vector.Id).Distinct().Order(StringComparer.Ordinal).ShouldEqual(FrozenHelperVectors.All.Select(vector => vector.Id).Order(StringComparer.Ordinal));
    }
}
