using DataTyped.Parser;
using Microsoft.CodeAnalysis;

namespace DataTyped.Model;

static class AdditionalFiles
{
    public class Directory
    {
        public bool IsRoot { get; set; }

        public string Name { get; set; }

        public List<File> Files { get; } = new();

        public List<Directory> SubDirectories { get; } = new();

        public override string ToString() => Name;
    }

    public record File(string FullPath, string RootPath)
    {
        public string FileName { get; } = Path.GetFileName(FullPath);

        public string RelativePath { get; } = FullPath.Replace(RootPath, "");

        public string RelativeDir { get; } = FullPath.Replace(RootPath, "").Replace(Path.GetFileName(FullPath), "");

        public string Identifier { get; } = DataTyped.Parser.Identifier.PropertyName(Path.GetFileName(FullPath));

        public string FullyQualifiedIdentifier
        {
            get
            {
                var result = "ProjectFiles.";
                result += RelativeDir.Replace(Path.DirectorySeparatorChar, '.');
                if (!result.EndsWith("."))
                    result += ".";
                result += Identifier;
                return result;
            }
        }
    }
    
    public static Directory Create(IEnumerable<AdditionalText> additionalTexts, string rootPath)
    {
        var items = CreateList(additionalTexts, rootPath);
        
        var rootDirectory = new Directory { Name = "ProjectFiles", IsRoot = true };

        foreach (var file in items)
        {
            var targetClass = GetTargetDirectory(file.RelativeDir, rootDirectory);

            targetClass.Files.Add(new File(file.Identifier, file.RelativePath));
        }

        return rootDirectory;
    }

    public static File[] CreateList(IEnumerable<AdditionalText> additionalTexts, string rootPath) => 
        additionalTexts.Select(x => new File(x.Path, rootPath)).ToArray();

    private static Directory GetTargetDirectory(string relativeDir, Directory parentDirectory)
    {
        if (string.IsNullOrEmpty(relativeDir))
            return parentDirectory;

        var parts = relativeDir.Split(Path.DirectorySeparatorChar);

        if (!parts.Any())
            return parentDirectory;

        var className = Identifier.PropertyName(parts[0]);

        var target =
            parentDirectory.SubDirectories.FirstOrDefault(x => x.Name == className);

        if (target is null)
        {
            target = new Directory { Name = className };
            parentDirectory.SubDirectories.Add(target);
        }

        var rest = string.Join(Path.PathSeparator.ToString(), parts.Skip(1));

        return GetTargetDirectory(rest, target);
    }
}
