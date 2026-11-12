// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Ui;
using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Registers the ability, component and setting catalogs of a model, which are what Stage's extension hosting answers from.
/// </summary>
public static class ExtensionModelHosting
{
    /// <summary>
    /// Replaces the empty catalogs with the ones the model and the loaded packages describe.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="application">The <see cref="SemanticApplication"/> of the model.</param>
    /// <param name="contract">The <see cref="UiContract"/> of the model.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>The <see cref="ExtensionComponentCatalog"/> the model places, which the scene needs too.</returns>
    /// <remarks>Register this before <see cref="PinnedAbilityCatalog.AddExtensionPackages"/>, which pins the abilities it describes to their packages.</remarks>
    public static ExtensionComponentCatalog Add(IServiceCollection services, SemanticApplication application, UiContract contract, IExtensionPackages packages)
    {
        var components = new ExtensionComponentCatalog(ExtensionModelCatalogs.Placements(contract, packages));
        services.Replace(ServiceDescriptor.Singleton<IAbilityCatalog>(new AbilityCatalog(ExtensionModelCatalogs.Abilities(application, packages))));
        services.Replace(ServiceDescriptor.Singleton<IExtensionComponentCatalog>(components));
        services.Replace(ServiceDescriptor.Singleton<ISystemSettingDeclarations>(new SystemSettingDeclarations(ExtensionModelCatalogs.Settings(application, packages))));
        services.Replace(ServiceDescriptor.Singleton<IScreenExtensionUsage>(ExtensionModelCatalogs.Usage(contract)));
        return components;
    }
}
