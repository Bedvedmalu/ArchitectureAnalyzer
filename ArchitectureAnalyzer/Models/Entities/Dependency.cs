namespace ArchitectureAnalyzer.Models.Entities
{
    public class Dependency
    {
        public int Id { get; init; }
        public int SourceClassId { get; set; }
        public int TargetClassId { get; set; }
        public CodeClass SourceClass { get; set; }
        public CodeClass TargetClass { get; set; }
        public string Type { get; set; } = "";

        public Dependency(CodeClass sourceClass, CodeClass targetClass, string type)
        {
            SourceClass = sourceClass;
            TargetClass = targetClass;
            Type = type;
        }
        private Dependency() { }
    }
}
