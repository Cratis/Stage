// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_settling_invocation_refusals : a_reaction_plan
{
    [Theory]
    [InlineData("on refused")]
    [InlineData("on refused by validation")]
    public async Task should_use_the_first_matching_branch_and_skip_the_remaining_invocations(string selector)
    {
        Compile($"""
            reaction Worker
              when Started
                invokes Complete
                  item = ""
                  {selector}
                    acknowledge
                  on refused by validation
                    produces Followed
                      item = "wrong branch"
                invokes Complete
                  item = "one"
            """, "then no events", new() { Authorization = "validate\n  item not empty message \"required\"" });
        await Execute();
        AssertMatchesReference();
    }

    [Theory]
    [InlineData("constraint Alpha", "Alpha", " first \nsecond")]
    [InlineData("constraint Beta", "Alpha", " first \nsecond")]
    [InlineData("constraint", "Alpha", " first \nsecond")]
    public async Task should_match_every_violated_name_but_expose_the_first_name(string selector, string first, string message)
    {
        Compile($"""
            reaction Worker
              when Started
                invokes Complete
                  item = item
                  on refused by {selector}
                    produces Followed
                      item = $refusal.constraint
            """, $"then Followed\n  for \"one\"\n  item = \"{first}\"", new()
        {
            Action = "given Completed\n  for \"one\"\nwhen append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1",
            Declarations = "constraint Beta\n  unique event Completed\n  message \"second\"\nconstraint Alpha\n  unique event Completed\n  message \" first \""
        });
        await Execute();
        AssertMatchesReference();
        var accepted = (SemanticAccepted)_reference.Execution;
        accepted.Facts.Length.ShouldEqual(2);

        // The separate message branch observes all violations, in candidate/sorted-name order.
        Compile($"""
            reaction Worker
              when Started
                invokes Complete
                  item = item
                  on refused by {selector}
                    produces Followed
                      item = $refusal.message
            """, "then Followed\n  item = \" first \\nsecond\"", new()
        {
            Action = "given Completed\n  for \"one\"\nwhen append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1",
            Declarations = "constraint Beta\n  unique event Completed\n  message \"second\"\nconstraint Alpha\n  unique event Completed\n  message \" first \""
        });
        await Execute();
        AssertMatchesReference();
        accepted = (SemanticAccepted)_reference.Execution;
        accepted.Facts[^1].Values[0].Value.ShouldEqual(Cratis.Screenplay.Semantics.SemanticValue.Text(message));
    }

    [Theory]
    [InlineData("any", "then denied")]
    [InlineData("validation", "then denied")]
    [InlineData("authorization", "then no events")]
    public async Task should_catch_authorization_only_with_the_explicit_selector(string selector, string expected)
    {
        var branch = selector == "any" ? "on refused" : $"on refused by {selector}";
        Compile($"reaction Worker\n  when Started\n    invokes Complete\n      item = item\n      {branch}\n        acknowledge", expected,
            new() { Policy = "policy Access\n  require role \"Automation\"", Authorization = "authorize Access" });
        await Execute();
        AssertMatchesReference();
    }

    [Fact]
    public async Task should_use_declared_system_roles_and_continue_after_a_successful_command()
    {
        Compile("reaction Worker\n  runs as system role \"Automation\"\n  when Started\n    invokes Complete\n      item = item\n      on refused by authorization\n        acknowledge", "then Completed\n  for \"one\"",
            new() { Policy = "policy Access\n  require role \"Automation\"", Authorization = "authorize Access" });
        await Execute();
        AssertMatchesReference();
    }

    [Fact]
    public async Task should_not_offer_a_rejected_branch_append_to_the_fallback()
    {
        Compile("""
            reaction Worker
              when Started
                invokes Complete
                  item = ""
                  on refused by validation
                    produces Followed
                      item = "again"
                  on refused
                    acknowledge
            """, "then error \"only once\"", new()
        {
            Authorization = "validate\n  item not empty message \"required\"",
            Action = "given Followed\n  for \"one\"\n  item = \"before\"\nwhen append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1",
            Declarations = "constraint OnlyOnce\n  unique event Followed\n  message \"only once\""
        });
        await Execute();
        AssertMatchesReference();
    }

    [Theory]
    [InlineData("command")]
    [InlineData("require")]
    public async Task should_preserve_the_exact_validation_message(string origin)
    {
        var rule = origin == "require" ? "validate\n  require item != \"\"\n    message \"  exact  \"" : "validate\n  item not empty message \"  exact  \"";
        Compile("reaction Worker\n  when Started\n    invokes Complete\n      item = \"\"\n      on refused by validation\n        produces Followed\n          item = $refusal.message", "then Followed\n  item = \"  exact  \"", new()
        {
            Authorization = rule
        });
        await Execute();
        AssertMatchesReference();
    }
}
#endif
