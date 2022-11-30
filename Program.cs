using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

internal partial class Program
{
    private static readonly string _colorUnrecognized = "red";
    private static readonly Dictionary<string, string> _colorCodes = new Dictionary<string, string> {
        {"net48","black"},
        {"v4.8","black"},
        {"netstandard2.0","blue"},
        {"netcoreapp2.1","yellow"},
        {"netcoreapp3.0","yellow"},
        {"netcoreapp3.1","lightgreen"},
        {"net6.0","green"},
        {"v4.5","orange"},
        {"v4.7.2","orange"},
    };

    private static void Main(string[] args)
    {
        string rootFolder =  @"C:\dotnet-application-root-folder";
        (Reference[] references, Project[] projects) = GetReferences(rootFolder);
        GenerateForVis(references, projects);
    }

    private static string GetNodesJson(Project[] projects)
    {
        StringBuilder nodes = new StringBuilder();
        nodes.AppendLine("[");
        List<string> nodesLines = new List<string>();
        foreach (var p in projects)
        {
            string color = _colorUnrecognized;
            string fw = p.targetFramework ?? p.targetFrameworkVersion ?? "";
            if (_colorCodes.ContainsKey(fw))
            {
                color = _colorCodes[fw];
            }
            string colorJson = color == "" ? "" : $@", ""color"": {{ ""border"": ""{color}"", ""background"": ""white"", ""highlight"": {{ ""border"": ""{color}"", ""background"": ""white"" }} }}";
            nodesLines.Add($@"  {{ ""id"": ""{JsonEncodedText.Encode(p.id)}"", ""label"": ""{JsonEncodedText.Encode(p.name)}""{colorJson} }}");
        };
        nodes.AppendLine(string.Join(",\n", nodesLines));
        nodes.Append("]");
        return nodes.ToString();
    }

    private static string GetEdgesJson(Reference[] references, Project[] projects)
    {
        StringBuilder edges = new StringBuilder();
        edges.AppendLine("[");
        List<string> edgesLines = new List<string>();
        foreach (var r in references)
        {
            edgesLines.Add($@"  {{ ""from"": ""{JsonEncodedText.Encode(r.b)}"", ""to"": ""{JsonEncodedText.Encode(r.a)}"" }}");

        }
        edges.AppendLine(string.Join(",\n", edgesLines));
        edges.Append("]");
        return edges.ToString();
    }

    private static string GetLabelsJson(Reference[] references, Project[] projects)
    {
        StringBuilder labels = new StringBuilder();
        labels.AppendLine("{");
        List<string> labelsLines = new List<string>();
        foreach (var cc in _colorCodes)
        {
            labelsLines.Add($@"  ""{cc.Key}"": ""border: 2px solid {cc.Value}""");
        }
        labelsLines.Add($@"  ""other values"": ""border: 2px solid {_colorUnrecognized}""");
        labels.AppendLine(string.Join(",\n", labelsLines));
        labels.Append("}");
        return labels.ToString();
    }

    private static void GenerateForVis(Reference[] references, Project[] projects)
    {
        var nodesJson = GetNodesJson(projects);
        var egesJson = GetEdgesJson(references, projects);
        var labelsJson = GetLabelsJson(references, projects);
        File.WriteAllText("output/nodes.json", nodesJson);
        File.WriteAllText("output/edges.json", egesJson);
        File.WriteAllText("output/labels.json", labelsJson);
    }

    private static (Reference[] references, Project[]) GetReferences(string rootFolder)
    {
        List<Reference> references = new List<Reference>();
        List<Project> projects = new();
        bool exportFileNamesOnly = true;
        int len = rootFolder.Length + 1;
        var files = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories).ToArray();
        foreach (var file in files)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNodeList projectReferences = doc.GetElementsByTagName("ProjectReference");
            XmlNode targetFramework = doc.GetElementsByTagName("TargetFramework")[0];
            XmlNode targetFrameworkVersion = doc.GetElementsByTagName("TargetFrameworkVersion")[0];
            var dirName = Path.GetDirectoryName(file);
            var a = file.Substring(len);
            Console.WriteLine($"Processed: {a}");
            foreach (XmlNode projectReference in projectReferences)
            {
                var b = Path.GetFullPath($"{dirName}\\{projectReference.Attributes["Include"].Value}").Substring(len);
                references.Add(new Reference(a, b));
            }
            projects.Add(
                new Project(
                    exportFileNamesOnly ? Path.GetFileNameWithoutExtension(a) : a,
                    a,
                    targetFramework?.InnerText, targetFrameworkVersion?.InnerText)
            );
        }
        return (references.ToArray(), projects.ToArray());
    }
}

internal record Reference(string a, string b);
internal record Project(string name, string id, string targetFramework, string targetFrameworkVersion);
