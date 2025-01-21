using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;

var bundleCommand = new Command("bundle", "bundle code files to a single file");
bundleCommand.AddAlias("b");

var outputOption = new Option<FileInfo>("--output", "file path and name") { IsRequired = true };
outputOption.AddAlias("o");

var languageOption = new Option<string>("--language", "Specify programming languages or 'all' for all files") { IsRequired = true };
languageOption.AddAlias("l");
var noteOption = new Option<bool>("--note", "Include source code as a comment in the bundle");
noteOption.AddAlias("n");
var sortOption = new Option<string>("--sort", "Sort files by 'name' (default) or 'type'")
{
    Arity = ArgumentArity.ZeroOrOne // ברירת מחדל היא לפי א"ב של שם הקובץ
};
sortOption.AddAlias("s");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from code");
removeEmptyLinesOption.AddAlias("r");
var authorOption = new Option<string>("--author", "Name of the author");
authorOption.AddAlias("a");
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((FileInfo output, string language, bool includeNote, string sort, bool removeEmptyLines, string author) =>
{
    try
    {
        // הגדרת שפות נתמכות
        string[] supportedLanguages = { "c#", "java", "react", "angular", "python", "c++", "c", "javascript", "dotnet" };
        string[] supportedEndLanguages = {
                                            "cs",    // C#
                                            "java",  // Java
                                            "jsx",   // React (JavaScript XML)
                                            "ts",    // Angular (TypeScript)
                                            "py",    // Python
                                            "cpp",   // C++
                                            "c",     // C
                                            "js",    // JavaScript
                                            "sln"    // .NET (לרוב קבצים מבוססי .NET)
                                        }; // הוספת סוגריים סוגרים

        List<string> filesToBundle;

        if (language.ToLower() == "all")
        {
            Console.WriteLine("Including all code files in the directory.");
            filesToBundle = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains("bin") && !f.Contains("debug")).ToList(); // הסרת סינון לפי סיומות
        }
        else if (supportedLanguages.Contains(language.ToLower())) // השוואה באותיות קטנות
        {
            // קביעת הסיומת של הקבצים לפי השפה
            string extension = language.ToLower() switch // השוואה באותיות קטנות
            {
                "c#" => "*.cs",
                "java" => "*.java",
                "react" => "*.jsx",
                "angular" => "*.ts",
                "python" => "*.py",
                "c++" => "*.cpp",
                "c" => "*.c",
                "javascript" => "*.js",
                "dotnet" => "*.sln", // תיקון לסיומת .dll
                _ => "*.*"
            };
            // קבלת הקבצים לפי הסיומת
            filesToBundle = Directory.GetFiles(Directory.GetCurrentDirectory(), extension, SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains("bin") && !f.Contains("debug")).ToList();
        }
        else
        {
            Console.WriteLine($"Unsupported language: {language}");
            return; // יציאה אם השפה לא נתמכת
        }
        // מיון קבצים לפי שם או סוג
        if (sort == "type")
        {
            filesToBundle = filesToBundle.OrderBy(f => Path.GetExtension(f)).ToList();
        }
        else
        {
            filesToBundle = filesToBundle.OrderBy(f => Path.GetFileName(f)).ToList();
        }
        // פתיחת קובץ הבנדל לכתיבה
        using var bundleFile = new StreamWriter(output.FullName);

        // הוספת הערות לקבצים אם נדרש
        if (includeNote)
        {
            foreach (var file in filesToBundle)
            {
                bundleFile.WriteLine($"// Source: {file}");
            }
        }

        // קריאת הקבצים וכתיבתם לקובץ הבנדל
        foreach (var file in filesToBundle)
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                // הסרת שורות ריקות אם נדרש
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                {
                    continue; // קפיצה לשורה הבאה
                }
                bundleFile.WriteLine(line); // כתיבת השורה לקובץ
            }
        }

        // הוספת שם המחבר אם נדרש
        if (!string.IsNullOrEmpty(author))
        {
            bundleFile.WriteLine($"// Author: {author}");
        }

        Console.WriteLine($"Bundling completed. Output file: {output.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}"); // טיפול בשגיאות
    }
}, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.SetHandler(async () =>
{
    Console.Write("Enter output file name (with .rsp extension): ");
    string outputFileName = Console.ReadLine();

    Console.WriteLine("c# / java / react / angular / python / c++ / c / javascript / dotnet / all");
    Console.Write("Enter language (or 'all'): ");
    string language = Console.ReadLine();

    Console.Write("Include note? (true/false): ");
    bool includeNote = bool.TryParse(Console.ReadLine(), out var result) && result;

    Console.Write("Sort by (name/type): ");
    string sort = Console.ReadLine();

    Console.Write("Remove empty lines? (true/false): ");
    bool removeEmptyLines = bool.TryParse(Console.ReadLine(), out result) && result;

    Console.Write("Enter author's name: ");
    string author = Console.ReadLine();

    // יצירת הפקודה המלאה
    string command = $"b o {outputFileName} l {language} n {includeNote} s {sort} r {removeEmptyLines} a \"{author}\"";

    // שמירת הפקודה בקובץ תגובה
    await File.WriteAllTextAsync(outputFileName, command);

    Console.WriteLine($"Response file created: {outputFileName}");
});

var rootCommand = new RootCommand("Root command for bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);
