namespace TrainTrack.Domain.Tests;

public class ProjectSetupTests
{
    [Fact]
    public void Test_project_references_Domain_assembly()
    {
        var assembly = typeof(DomainAssemblyMarker).Assembly;

        assembly.GetName().Name.ShouldBe("TrainTrack.Domain");
    }
}
