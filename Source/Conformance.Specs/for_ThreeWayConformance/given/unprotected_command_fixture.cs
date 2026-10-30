// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

// Screenplay rejects advisory reads, so this customization is deliberately hand-authored, not rendered.
internal static class unprotected_command_fixture
{
    internal const string Command = """
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Arc.Commands.ModelBound;

        namespace ProtectedReads.Customizations;

        [Command]
        [Unprotected]
        public record Inspect
        {
            public void Handle() { }
        }
        """;

    internal const string Specification = """
        using System.Reflection;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Arc.Commands;
        using Cratis.Specifications;
        using ProtectedReads.Customizations;
        using ProtectedReads.Storage.Decisions.Use;
        using Xunit;

        namespace ProtectedReads.Probes;

        public class when_compiling_an_unprotected_command : Specification
        {
            [Fact] void should_enable_assembly_read_protection() => typeof(Consume).Assembly.GetCustomAttribute<ReadProtectionAttribute>()!.Mode.ShouldEqual(ReadProtectionMode.On);
            [Fact] void should_not_need_per_command_protection() => Assert.Null(typeof(Consume).GetCustomAttribute<ReadProtectionAttribute>());
            [Fact] void should_allow_a_hand_authored_opt_out() => Assert.NotNull(typeof(Inspect).GetCustomAttribute<UnprotectedAttribute>());
        }
        """;
}
#endif
