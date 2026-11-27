// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CodeProvidedReadModels.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CodeProvidedReadModels;

/// <summary>Builds the emitted application twice and runs its real execution-gate proof.</summary>
public class when_executing_the_invoicing_extraction : an_invoicing_extraction
{
    async Task Because()
    {
        AddGeneratedSpecification("when_deciding_invoice_availability.cs", InvoicingExecutionProof.Source);
        await Run("invoicing-debug-build.log", "build", "Invoicing.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        await Run("invoicing-debug-proof.log", "test", "Invoicing.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_deciding_invoice_availability");
        await Run("invoicing-release-build.log", "build", "Invoicing.csproj", "-c", "Release", "-warnaserror", "--nologo");
    }

    [Fact] void should_emit_the_providers() => Assert.True(GeneratedFileExists("GeneratedProvides/ProvidedValues.cs"));
    [Fact] void should_emit_the_real_gate_registration() => Assert.True(GeneratedFileExists("GeneratedAvailability/Availability.cs"));
}
#endif
