// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rerendering_a_reordered_enumeration : Specification
{
    string _original = null!;
    string _reordered = null!;

    void Because()
    {
        _original = Render(an_enumeration_model.Source);
        _reordered = Render(an_enumeration_model.Source.Replace("  draft\n  issued", "  issued\n  draft", StringComparison.Ordinal));
    }

    // Unnumbered enums retain their positional-renumbering hazard; numbered enums preserve model-owned identities.
    [Fact]
    void should_document_that_reordering_changes_the_implicit_member_numbers()
    {
        _original.IndexOf("Draft,", StringComparison.Ordinal).ShouldBeLessThan(_original.IndexOf("Issued,", StringComparison.Ordinal));
        _reordered.IndexOf("Issued,", StringComparison.Ordinal).ShouldBeLessThan(_reordered.IndexOf("Draft,", StringComparison.Ordinal));
    }
    [Fact] void should_keep_unnumbered_members_implicit() => (_original.Contains('=') || _reordered.Contains('=')).ShouldBeFalse();

    static string Render(string source)
    {
        var plan = invoice_model.Plan(an_enumeration_model.Compile(source));
        plan.Success.ShouldBeTrue();

        return Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Common/Status.cs").Bytes.AsSpan());
    }
}
