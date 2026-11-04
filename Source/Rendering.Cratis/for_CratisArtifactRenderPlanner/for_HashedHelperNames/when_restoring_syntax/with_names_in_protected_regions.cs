// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.for_HashedHelperNames.when_restoring_syntax;

public class with_names_in_protected_regions : Specification
{
    const string Source = """"
        // Custom is a comment about CustomForIssueInvoice
        class PolicyBodies
        {
            const string Name = "CustomForIssueInvoice";
            const string Verbatim = @"CustomForIssueInvoice";
            const string Raw = """CustomForIssueInvoice""";
            static string Interpolated(int x) => $"{x} CustomForIssueInvoice";
            static bool Evaluate_Custom(CustomContext context) => CustomForIssueInvoice(context);
            static bool CustomForIssueInvoice(CustomContext context)
            {
                var CustomForIssueInvoice2 = CustomForIssueInvoice(context);
                return Other.CustomForIssueInvoice(context);
            }
            static IEnumerable<string> Validate_Core(CustomContext context) { checked { var x = Validate_Core(context); } return null; }
            static IEnumerable<string> Validate(CustomContext context) { checked { return Validate_Core(context); } }
        }
        """";

    static readonly HashedHelperNames.Entry[] Entries =
    [
        new("CustomContext", "TypedContext_1", HashedHelperNames.Protection.None),
        new("Evaluate_Custom", "Evaluate_1", HashedHelperNames.Protection.None),
        new("CustomForIssueInvoice", "Body_1", HashedHelperNames.Protection.Body),
        new("Validate_Core", "Validate_1_Core", HashedHelperNames.Protection.Checked),
        new("Validate", "Validate_1", HashedHelperNames.Protection.None)
    ];

    string _restored = null!;

    void Because() => _restored = HashedHelperNames.RestoreSyntax(Source, Entries);

    [Fact] void should_restore_generated_declarations_and_references() => Assert.Contains("static bool Evaluate_1(TypedContext_1 context) => Body_1(context);", _restored, StringComparison.Ordinal);
    [Fact] void should_restore_the_declaring_method_of_an_authored_body() => Assert.Contains("static bool Body_1(TypedContext_1 context)", _restored, StringComparison.Ordinal);
    [Fact] void should_keep_the_authored_body_bytes() => Assert.Contains("var CustomForIssueInvoice2 = CustomForIssueInvoice(context);\n        return Other.CustomForIssueInvoice(context);", _restored, StringComparison.Ordinal);
    [Fact] void should_keep_comments() => Assert.Contains("// Custom is a comment about CustomForIssueInvoice", _restored, StringComparison.Ordinal);
    [Fact] void should_keep_every_string_form() => Assert.Contains("const string Name = \"CustomForIssueInvoice\";\n    const string Verbatim = @\"CustomForIssueInvoice\";\n    const string Raw = \"\"\"CustomForIssueInvoice\"\"\";\n    static string Interpolated(int x) => $\"{x} CustomForIssueInvoice\";", _restored, StringComparison.Ordinal);
    [Fact] void should_keep_the_checked_block_of_a_validation_core() => Assert.Contains("checked { var x = Validate_Core(context); }", _restored, StringComparison.Ordinal);
    [Fact] void should_restore_the_validation_forwarding_call() => Assert.Contains("static IEnumerable<string> Validate_1(TypedContext_1 context) { checked { return Validate_1_Core(context); } }", _restored, StringComparison.Ordinal);
    [Fact] void should_fail_on_ambiguous_mappings() => Assert.Throws<InvalidOperationException>(() => HashedHelperNames.RestoreSyntax(Source, [.. Entries, new("Evaluate_Custom", "Other", HashedHelperNames.Protection.None)]));
    [Fact] void should_leave_unaffected_text_unchanged() => HashedHelperNames.RestoreSyntax("class A { void M() { var x = 1; } }", Entries).ShouldEqual("class A { void M() { var x = 1; } }");
}
#endif
