using System.Reflection;
using NetArchTest.Rules;
using ArchTestResult = NetArchTest.Rules.TestResult;
using TrainTrack.Application;
using TrainTrack.Domain;
using TrainTrack.Infrastructure;

namespace TrainTrack.Architecture.Tests;

/// <summary>Kierunek zależności między warstwami wg ADR-002.</summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "TrainTrack.Domain";
    private const string ApplicationNamespace = "TrainTrack.Application";
    private const string InfrastructureNamespace = "TrainTrack.Infrastructure";
    private const string ApiNamespace = "TrainTrack.Api";
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private static readonly Assembly DomainAssembly = typeof(DomainAssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InfrastructureAssemblyMarker).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_other_layers_or_frameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace, EntityFrameworkCore, AspNetCore)
            .GetResult();

        FailingTypes(result).ShouldBeEmpty();
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_api_or_frameworks()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, EntityFrameworkCore, AspNetCore)
            .GetResult();

        FailingTypes(result).ShouldBeEmpty();
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace)
            .GetResult();

        FailingTypes(result).ShouldBeEmpty();
    }

    // Nazwy typów łamiących regułę trafiają do komunikatu błędu, więc od razu widać, co poprawić.
    private static IEnumerable<string> FailingTypes(ArchTestResult result) =>
        result.FailingTypes?.Select(type => type.FullName) ?? [];
}
