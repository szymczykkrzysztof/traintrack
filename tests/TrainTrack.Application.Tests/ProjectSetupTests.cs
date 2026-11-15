namespace TrainTrack.Application.Tests;

public class ProjectSetupTests
{
    [Fact]
    public void Test_project_references_Application_assembly()
    {
        var assembly = typeof(ApplicationAssemblyMarker).Assembly;

        assembly.GetName().Name.ShouldBe("TrainTrack.Application");
    }
}
