// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_StreamIdsRendering;

public class when_reading_shared_codec_vectors : given.a_compiled_codec
{
    readonly List<string> _differences = [];

    void Because()
    {
        foreach (var vector in _vectors.RootElement.GetProperty("scalars").EnumerateArray())
        {
            var kind = vector.GetProperty("kind").GetString();
            var input = Input(vector);
            if (kind == "String")
            {
                var valid = Text(input, out var text);
                var expected = vector.TryGetProperty("canonical", out var canonical);
                if (valid != expected || (valid && text != canonical.GetString())) _differences.Add(vector.ToString());
            }
            else if (kind == "Uuid" && vector.TryGetProperty("canonical", out var uuid))
            {
                if ((string)Invoke("Uuid", Guid.Parse(input)) != uuid.GetString()) _differences.Add(vector.ToString());
            }
            else if (kind == "Int" && long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) && vector.TryGetProperty("canonical", out var number))
            {
                if ((string)Invoke("Integer", integer) != number.GetString()) _differences.Add(vector.ToString());
            }

            // Malformed UUIDs and integers outside Int64 are binder/literal admission vectors;
            // generated handlers receive a Guid or long, never authored scalar text.
        }
        foreach (var vector in _vectors.RootElement.GetProperty("composites").EnumerateArray())
        {
            var parts = vector.GetProperty("parts").EnumerateArray().Select(part => part.GetString()).ToArray();
            if ((string)Invoke("Composite", (object)parts) != vector.GetProperty("encoded").GetString()) _differences.Add(vector.ToString());
        }
    }

    [Fact] void should_match_every_applicable_shared_vector() => _differences.ShouldBeEmpty();
}
