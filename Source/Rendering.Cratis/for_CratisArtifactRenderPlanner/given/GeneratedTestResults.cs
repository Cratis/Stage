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
    /// Matches structural class and method identities, never runner display names.
    /// </summary>
    /// <param name="result">The executed result.</param>
    /// <param name="className">An exact class name, qualified or unqualified.</param>
    /// <param name="methodName">An exact method name.</param>
    /// <param name="match">Whether to match exact identities or their prefixes.</param>
    /// <returns>Whether every supplied identity criterion matches.</returns>
    public static bool Matches(this GeneratedTestResult result, string? className = null, string? methodName = null, GeneratedTestNameMatch match = GeneratedTestNameMatch.Exact) =>
        NameMatches(ClassIdentity(result.ClassName, className), className, match) && NameMatches(result.MethodName, methodName, match);

    /// <summary>
    /// Matches an exact fully qualified class and method identity.
    /// </summary>
    /// <param name="result">The executed result.</param>
    /// <param name="fullName">The fully qualified class and method name.</param>
    /// <returns>Whether the structural identity matches.</returns>
    public static bool Matches(this GeneratedTestResult result, string fullName) => $"{result.ClassName}.{result.MethodName}" == fullName;

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

    static string ClassIdentity(string actual, string? expected) =>
        expected?.Contains('.') != false ? actual : actual[(actual.LastIndexOf('.') + 1)..];

    static bool NameMatches(string actual, string? expected, GeneratedTestNameMatch match) =>
        expected is null || (match == GeneratedTestNameMatch.Prefix ? actual.StartsWith(expected, StringComparison.Ordinal) : actual == expected);
}
#endif
