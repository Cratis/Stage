// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Names an ability of a system at the major version of its contract.
/// </summary>
/// <param name="System">The name of the system that declares the ability.</param>
/// <param name="Ability">The name of the ability.</param>
/// <param name="ContractMajor">The major version of the ability's contract.</param>
public sealed record AbilityReference(string System, string Ability, int ContractMajor)
{
    /// <summary>
    /// Gets the prefix every ability capability id starts with.
    /// </summary>
    public const string Prefix = "ability:";

    /// <summary>
    /// Gets the id of the capability the ability is performed under, <c lang="text">ability:&lt;System&gt;.&lt;Ability&gt;</c>. It carries no version, so a capability
    /// limit covers every contract major.
    /// </summary>
    public string CapabilityId => $"{Prefix}{System}.{Ability}";

    /// <summary>
    /// Gets the capability, <c lang="text">ability:&lt;System&gt;.&lt;Ability&gt;@&lt;contract major&gt;</c>.
    /// </summary>
    public string Capability => $"{CapabilityId}@{ContractMajor}";

    /// <summary>
    /// Gets the id of the workflow that runs one invocation of the ability.
    /// </summary>
    public string Workflow => CapabilityId;

    /// <summary>
    /// Splits a capability id into the system and the ability.
    /// </summary>
    /// <param name="capabilityId">The capability id, <c lang="text">ability:&lt;System&gt;.&lt;Ability&gt;</c>.</param>
    /// <param name="system">The system, when the id is an ability capability.</param>
    /// <param name="ability">The ability, when the id is an ability capability.</param>
    /// <returns>True when the id names an ability.</returns>
    public static bool TryParse(string capabilityId, out string system, out string ability)
    {
        system = string.Empty;
        ability = string.Empty;
        if (!capabilityId.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var name = capabilityId[Prefix.Length..];
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == name.Length - 1) return false;
        system = name[..dot];
        ability = name[(dot + 1)..];
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Capability;
}
