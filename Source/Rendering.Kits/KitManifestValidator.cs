// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Stage.Rendering.Kits;

internal static partial class KitManifestValidator
{
    [GeneratedRegex(@"^[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex TargetIdPattern { get; }

    [GeneratedRegex(@"^[A-Z][A-Z0-9]*(-[A-Z0-9]+)+\z", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex DiagnosticPattern { get; }

    internal static void Validate(KitManifest manifest, KitEngineCapabilities capabilities, List<KitDiagnostic> diagnostics)
    {
        ValidateApi(manifest.ApiVersion, capabilities, diagnostics);
        ValidateBindings(manifest, capabilities, diagnostics);
        ValidateDeclarations(manifest, diagnostics);
        ValidateDuplicates(manifest, diagnostics);
    }

    internal static IEnumerable<string> DeclaredFiles(KitManifest manifest) => manifest.Outputs.Select(output => output.Template)
        .Concat(manifest.Partials.Select(partial => partial.Template))
        .Concat(manifest.Tables.Select(table => table.Path))
        .Concat(manifest.Assets.Select(asset => asset.Path));

    static void ValidateApi(ContractVersion api, KitEngineCapabilities capabilities, List<KitDiagnostic> diagnostics)
    {
        if (!string.Equals(api.Name, "stage-target", StringComparison.Ordinal) || api.Major != 1)
        {
            diagnostics.Add(new(KitCodes.UnsupportedApi, "Expected stage-target/v1.MINOR.", "$.apiVersion"));
        }
        else if (api.Minor > capabilities.TargetApiMinor)
        {
            diagnostics.Add(new(KitCodes.NewerApi, "The target API minor is newer than the engine supports.", "$.apiVersion"));
        }
    }

    static void ValidateBindings(KitManifest manifest, KitEngineCapabilities capabilities, List<KitDiagnostic> diagnostics)
    {
        ValidatePacks(manifest, capabilities, diagnostics);
        ValidateContracts(manifest, capabilities, diagnostics);
    }

    static void ValidatePacks(KitManifest manifest, KitEngineCapabilities capabilities, List<KitDiagnostic> diagnostics)
    {
        foreach (var pack in manifest.LanguagePacks)
        {
            if (!LanguagePackVersion.TryParse(pack.ToString(), out _))
            {
                diagnostics.Add(new(KitCodes.InvalidLanguagePack, "An exact id@MAJOR.MINOR.PATCH language pack is required.", "$.languagePacks"));
            }
            else if (!capabilities.LanguagePacks.Contains(pack))
            {
                diagnostics.Add(new(KitCodes.MissingLanguagePack, $"The engine does not provide {pack}.", "$.languagePacks"));
            }
        }
    }

    static void ValidateContracts(KitManifest manifest, KitEngineCapabilities capabilities, List<KitDiagnostic> diagnostics)
    {
        foreach (var required in manifest.Requires)
        {
            var matches = capabilities.Contracts.Where(contract => string.Equals(contract.Name, required.Name, StringComparison.Ordinal) && contract.Major == required.Major).ToArray();
            if (required.Major < 0 || matches.Length == 0)
            {
                diagnostics.Add(new(KitCodes.MissingContract, $"The engine does not provide the required major of {required}.", "$.requires"));
            }
            else if (matches.All(contract => contract.Minor < required.Minor))
            {
                diagnostics.Add(new(KitCodes.NewerContract, $"The required minor of {required} is newer than the engine provides.", "$.requires"));
            }
        }
    }

    static void ValidateDeclarations(KitManifest manifest, List<KitDiagnostic> diagnostics)
    {
        if (!TargetIdPattern.IsMatch(manifest.Target.Id) || !LanguagePackVersion.IsExactVersion(manifest.Target.Version))
        {
            diagnostics.Add(new(KitCodes.InvalidDeclaration, "Target id or exact version is invalid.", "$.target"));
        }

        foreach (var declaration in manifest.Diagnostics.Where(declaration => !DiagnosticPattern.IsMatch(declaration.Code) || declaration.Code.StartsWith("STAGE-", StringComparison.Ordinal)))
        {
            diagnostics.Add(new(KitCodes.InvalidDeclaration, "Diagnostic codes must be kit-owned and cannot use STAGE-.", declaration.Code));
        }

        var requiredStrings = manifest.Outputs.SelectMany(output => new[] { output.Id, output.Path })
            .Concat(manifest.Outputs.SelectMany(output => new[] { output.ForEach, output.When }).OfType<string>())
            .Concat(manifest.Partials.Select(partial => partial.Name))
            .Concat(manifest.Tables.Select(table => table.Name))
            .Concat(manifest.Assets.Select(asset => asset.Output))
            .Concat(manifest.Diagnostics.Select(declaration => declaration.Summary));
        if (requiredStrings.Any(string.IsNullOrWhiteSpace))
        {
            diagnostics.Add(new(KitCodes.InvalidManifest, "Declaration strings must not be empty.", "$"));
        }

        foreach (var path in DeclaredFiles(manifest))
        {
            var code = KitPath.Validate(path);
            if (code is not null)
            {
                diagnostics.Add(new(code, "Invalid declared file path.", path));
            }
        }
    }

    static void ValidateDuplicates(KitManifest manifest, List<KitDiagnostic> diagnostics)
    {
        Duplicates(manifest.Outputs.Select(output => output.Id), "$.outputs", diagnostics);
        Duplicates(manifest.Partials.Select(partial => partial.Name), "$.partials", diagnostics);
        Duplicates(manifest.Tables.Select(table => table.Name), "$.tables", diagnostics);
        Duplicates(manifest.Diagnostics.Select(declaration => declaration.Code), "$.diagnostics", diagnostics);
        Duplicates(manifest.LanguagePacks.Select(pack => pack.Id), "$.languagePacks", diagnostics);
        Duplicates(manifest.Requires.Select(contract => contract.Name), "$.requires", diagnostics);
        Duplicates(DeclaredFiles(manifest), "$", diagnostics);
        Duplicates(manifest.Outputs.Select(output => output.Path).Concat(manifest.Assets.Select(asset => asset.Output)), "$.outputs", diagnostics);
    }

    static void Duplicates(IEnumerable<string> values, string path, List<KitDiagnostic> diagnostics)
    {
        foreach (var group in values.GroupBy(value => value, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(new(KitCodes.DuplicateDeclaration, $"Duplicate declaration: {group.Key}.", path));
        }
    }
}
