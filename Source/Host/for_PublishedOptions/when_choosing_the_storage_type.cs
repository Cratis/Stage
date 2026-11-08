// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

[PerFactSpecificationLifecycle]
public class when_choosing_the_storage_type : a_published_configuration
{
    [Fact] void should_store_in_mongodb_by_default() => Options().CanonicalStorageType.ShouldEqual(PublishedOptions.MongoDBStorage);
    [Fact] void should_project_into_mongodb_by_default() => Options().ReadModelSinkTypeId.ShouldEqual(WellKnownSinkTypes.MongoDB);

    [Fact]
    void should_store_in_postgresql_in_the_kernels_spelling()
    {
        _values["Stage:Published:Storage:Type"] = "postgresql";
        Options().CanonicalStorageType.ShouldEqual(PublishedOptions.PostgreSqlStorage);
    }

    [Fact]
    void should_store_in_sql_server_in_the_kernels_spelling()
    {
        _values["Stage:Published:Storage:Type"] = "MSSQL";
        Options().CanonicalStorageType.ShouldEqual(PublishedOptions.MsSqlStorage);
    }

    [Fact]
    void should_project_into_sql_for_postgresql()
    {
        _values["Stage:Published:Storage:Type"] = "PostgreSql";
        Options().ReadModelSinkTypeId.ShouldEqual(WellKnownSinkTypes.SQL);
    }

    [Fact]
    void should_project_into_sql_for_sql_server()
    {
        _values["Stage:Published:Storage:Type"] = "MsSql";
        Options().ReadModelSinkTypeId.ShouldEqual(WellKnownSinkTypes.SQL);
    }

    [Fact]
    void should_accept_a_sql_engine()
    {
        _values["Stage:Published:Storage:Type"] = "MsSql";
        Options().Problems().ShouldBeEmpty();
    }
}
