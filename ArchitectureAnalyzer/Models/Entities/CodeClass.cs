namespace ArchitectureAnalyzer.Models.Entities
{
    public class CodeClass
    {
        public int Id { get; init; }
        public int ProjectID { get; set; }
        public string ClassName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public int CyclomaticComplexity { get; set; }
        public int LinesCount { get; set; }

        public CodeClass(int projectID, string className, string @namespace, int complexity, int lines)
        {
            ProjectID = projectID;
            ClassName = className;
            Namespace = @namespace;
            CyclomaticComplexity = complexity;
            LinesCount = lines;
        }

        private CodeClass() { }
        public bool IsComplex() => CyclomaticComplexity > Constants.RequiredCyclomaticComplexity;
        public string GetRiskLevel()
        {
            if (CyclomaticComplexity > Constants.RequiredCyclomaticComplexity * 1.5) return "Critical";
            if (CyclomaticComplexity > Constants.RequiredCyclomaticComplexity)  return "High";
            if (CyclomaticComplexity > Constants.RequiredCyclomaticComplexity * 0.5) return "Medium";
            return "Low";
        }
    }
}
