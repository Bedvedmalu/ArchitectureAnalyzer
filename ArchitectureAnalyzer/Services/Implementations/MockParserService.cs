using ArchitectureAnalyzer.Models.Entities;
using ArchitectureAnalyzer.Services.Interfaces;

namespace ArchitectureAnalyzer.Services.Implementations
{
    public class MockParserService : IParserService
    {
        public Task<Project> ParseAsync(string ProjectPath)
        {
            var Project = new Project("TestProject", "C#");

            var class1 = new CodeClass(0, "TestProject_Class1", "MyApp.Services", 10, 150);
            var class2 = new CodeClass(0, "TestProject_Class2", "MyApp.Services", 15, 350);
            var class3 = new CodeClass(0, "TestProject_Class3", "MyApp.Services", 8, 50);

            Project.AddClass(class1);
            Project.AddClass(class2);
            Project.AddClass(class3);

            var dependency1 = new Dependency(class1, class2, "Aggregation");
            var dependency2 = new Dependency(class2, class3, "Another");

            Project.AddDependency(dependency1);
            Project.AddDependency(dependency2);

            return Task.FromResult(Project);
        }
    }
}
