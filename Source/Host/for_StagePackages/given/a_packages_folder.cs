// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.for_StagePackages.given;

/// <summary>
/// A folder holding an approved package set and the browser build of its one package.
/// </summary>
public class a_packages_folder : Specification
{
    protected const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [\"@acme/scene.charts/styles.css\"] } }" +
        "] }";

    protected string Root = null!;

    void Establish()
    {
        Root = Directory.CreateTempSubdirectory("stage-packages-spec").FullName;
        File.WriteAllText(Path.Combine(Root, StagePackages.SetFileName), Document);
        var package = Directory.CreateDirectory(Path.Combine(Root, "@acme", "scene.charts")).FullName;
        File.WriteAllText(Path.Combine(package, "package.json"), "{ \"name\": \"@acme/scene.charts\", \"exports\": { \".\": { \"import\": \"./dist/index.js\" } } }");
        Directory.CreateDirectory(Path.Combine(package, "dist"));
        File.WriteAllText(Path.Combine(package, "dist", "index.js"), "export const acmeChartsRuntimePackage = {};");
        File.WriteAllText(Path.Combine(package, "styles.css"), ".acme-chart { color: red; }");
        File.WriteAllText(Path.Combine(package, "secret.txt"), "not served");

        // A package the set does not approve, sitting beside the approved ones.
        var other = Directory.CreateDirectory(Path.Combine(Root, "@other", "package")).FullName;
        File.WriteAllText(Path.Combine(other, "index.js"), "export default 1;");
    }

    void Destroy() => Directory.Delete(Root, recursive: true);
}
