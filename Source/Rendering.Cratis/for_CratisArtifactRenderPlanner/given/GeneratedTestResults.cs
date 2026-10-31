// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Reads generated test outcomes by joining TRX results to their method definitions.
/// </summary>
public static class GeneratedTestResults
{
    /// <summary>
    /// Reads every result in report order without interpreting runner-specific display names.
    /// </summary>
    /// <param name="document">The TRX report.</param>
    /// <returns>The structural identities and outcomes.</returns>
    public static ImmutableArray<GeneratedTestResult> Read(XDocument document)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var definitions = document.Descendants(ns + "TestDefinitions").Elements(ns + "UnitTest")
            .ToDictionary(test => (string)test.Attribute("id")!, test => test.Element(ns + "TestMethod")!, StringComparer.Ordinal);
        return [.. document.Descendants(ns + "UnitTestResult").Select(result =>
        {
            var id = (string)result.Attribute("testId")!;
            var method = definitions[id];
            return new GeneratedTestResult(id, (string)method.Attribute("className")!, (string)method.Attribute("name")!, (string)result.Attribute("outcome")!);
        })];
    }
}
#endif
