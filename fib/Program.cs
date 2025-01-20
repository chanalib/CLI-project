using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;

void CopyFileToOutput(string filePath, FileInfo output, bool removeEmptyLines, string author, bool includeNote)
{
    try
    {
        string destinationPath = Path.Combine(output.DirectoryName, Path.GetFileName(filePath));
        var content = File.ReadAllLines(filePath);

        // הסרת שורות ריקות אם נדרש
        if (removeEmptyLines)
        {
            content = content.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        }

        // הוספת שם הקובץ כהערה אם נדרש
        if (includeNote)
        {
            var note = $"// Source: {Path.GetFileName(filePath)}\n";
            File.AppendAllText(destinationPath, note);
        }

        // הוספת שם היוצר כהערה בראש הקובץ
        if (!string.IsNullOrEmpty(author))
        {
            var authorNote = $"// Author: {author}\n";
            File.AppendAllText(destinationPath, authorNote);
        }

        // כתיבת תוכן הקובץ ליעד
        File.AppendAllLines(destinationPath, content);
        Console.WriteLine($"Copied {filePath} to {destinationPath}");
    }
    catch (IOException ioEx)
    {
        Console.WriteLine($"Error copying file: {ioEx.Message}");
    }
}

var bundleCommand = new Command("bundle", "bundle code files to a single file");
var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

var bundleOutputOption = new Option<FileInfo>("--output", "file path and name") { IsRequired = true };
bundleOutputOption.AddAlias("o");
var bundleLanguageOption = new Option<string>("--language", "Specify programming languages or 'all' for all files") { IsRequired = true };
bundleLanguageOption.AddAlias("l");
var noteOption = new Option<bool>("--note", "Include source code as a comment in the bundle");
noteOption.AddAlias("n");
var sortOption = new Option<string>("--sort", "Sort files by 'name' (default) or 'type'");
sortOption.AddAlias("s");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from code");
removeEmptyLinesOption.AddAlias("r");
var authorOption = new Option<string>("--author", "Name of the author");
authorOption.AddAlias("a");

bundleCommand.AddOption(bundleOutputOption);
bundleCommand.AddOption(bundleLanguageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((FileInfo output, string language, bool includeNote, string sort, bool removeEmptyLines, string author) =>
{
    try
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        var filesToCopy = new List<string>();

        if (language.ToLower() == "all")
        {
            Console.WriteLine("Including all code files in the directory.");
            filesToCopy = Directory.GetFiles(currentDirectory)
                .Where(file => !file.Contains("\\bin\\") && !file.Contains("\\debug\\"))
                .ToList();
        }
        else
        {
            Console.WriteLine($"Including code files for language: {language}");
            filesToCopy = Directory.GetFiles(currentDirectory, $"*.{language}")
                .Where(file => !file.Contains("\\bin\\") && !file.Contains("\\debug\\"))
                .ToList();
        }

        // מיון קבצים לפי שם או סוג
        if (sort == "type")
        {
            filesToCopy = filesToCopy.OrderBy(f => Path.GetExtension(f)).ToList();
        }
        else
        {
            filesToCopy = filesToCopy.OrderBy(f => Path.GetFileName(f)).ToList();
        }

        foreach (var file in filesToCopy)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"Error: File {file} does not exist.");
                continue;
            }

            // בדוק אם הקובץ הוא קובץ קוד (למשל, קבצי .cs)
            if (!file.EndsWith($".{language}"))
            {
                Console.WriteLine($"Skipping file {file} as it does not match the specified language.");
                continue;
            }

            CopyFileToOutput(file, output, removeEmptyLines, author, includeNote);
        }
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: File path is invalid");
    }
}, bundleOutputOption, bundleLanguageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);


createRspCommand.SetHandler(async () =>
{
    Console.Write("Enter output file name (with .rsp extension): ");
    string outputFileName = Console.ReadLine();

    Console.Write("Enter language (or 'all'): ");
    string language = Console.ReadLine();

    Console.Write("Include note? (true/false): ");
    bool includeNote = bool.Parse(Console.ReadLine());

    Console.Write("Sort by (name/type): ");
    string sort = Console.ReadLine();

    Console.Write("Remove empty lines? (true/false): ");
    bool removeEmptyLines = bool.Parse(Console.ReadLine());

    Console.Write("Enter author name: ");
    string author = Console.ReadLine();

    // יצירת הפקודה המלאה
    string command = $"bundle o {outputFileName} l {language} n {includeNote} s {sort} r {removeEmptyLines} a \"{author}\"";

    // שמירת הפקודה בקובץ תגובה
    await File.WriteAllTextAsync(outputFileName, command);

    Console.WriteLine($"Response file created: {outputFileName}");
});

var rootCommand = new RootCommand("Root command for bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);

