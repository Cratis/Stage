// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_the_connection_comes_from_a_mounted_secret : a_published_configuration
{
    string _file = null!;
    PublishedOptions _options = null!;

    void Establish()
    {
        _file = Path.GetTempFileName();
        File.WriteAllText(_file, "mongodb://from-secret:27017\n");
        _values.Remove("Stage:Published:Storage:ConnectionString");
        _values["Stage:Published:Storage:ConnectionStringFile"] = _file;
    }

    void Because() => _options = Options();

    void Destroy() => File.Delete(_file);

    [Fact] void should_read_the_secret_file() => _options.StorageConnectionString.ShouldEqual("mongodb://from-secret:27017");
    [Fact] void should_be_complete() => _options.Problems().ShouldBeEmpty();
}
