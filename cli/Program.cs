
using System.CommandLine;

var languageOption = new Option<string>(
    new[] { "--language", "-l" },
    "Programming language(s) to bundle (e.g., c#, java, js). Use 'all' to include all files.")
{
    IsRequired = true
};

var outputOption = new Option<FileInfo>(
    new[] { "--output", "-o" },
    "File path and name for the bundled output")
{
    IsRequired = true
};

var noteOption = new Option<bool>(
    new[] { "--note", "-n" },
    "Include the source file's relative path as a comment in the bundle");

var sortOption = new Option<string>(
    new[] { "--sort", "-s" },
    () => "name",
    "Sort files by 'name' or 'type'");

var removeEmptyLinesOption = new Option<bool>(
    new[] { "--remove-empty-lines", "-r" },
    "Remove empty lines from files");

var authorOption = new Option<string>(
    new[] { "--author", "-a" },
    "Include the author's name in the bundle as a comment");

var bundleCommand = new Command("bundle", "Bundle code files into a single file")
{
    languageOption,
    outputOption,
    noteOption,
    sortOption,
    removeEmptyLinesOption,
    authorOption
};

bundleCommand.SetHandler(
    (string language, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
    {
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var excludedDirs = new[] { "bin", "obj", "debug", "release" };

            var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
                .Where(file => !excludedDirs.Any(dir => file.Contains(dir)))
                .Where(file => language.ToLower() == "all" || file.EndsWith($".{language}", StringComparison.OrdinalIgnoreCase));

            if (!files.Any())
            {
                Console.WriteLine("No files found to bundle for the specified language.");
                return;
            }

            files = sort switch
            {
                "type" => files.OrderBy(f => Path.GetExtension(f)).ToList(),
                _ => files.OrderBy(f => Path.GetFileName(f)).ToList()
            };

            using var writer = new StreamWriter(output.FullName);

            if (!string.IsNullOrWhiteSpace(author))
            {
                writer.WriteLine($"// Author: {author}");
            }

            foreach (var file in files)
            {
                if (note)
                {
                    writer.WriteLine($"// Source: {Path.GetRelativePath(currentDirectory, file)}");
                }

                var content = File.ReadAllLines(file);
                if (removeEmptyLines)
                {
                    content = content.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                writer.WriteLine($"// Start of file: {Path.GetFileName(file)}");
                writer.Write(string.Join(Environment.NewLine, content));
                writer.WriteLine($"// End of file: {Path.GetFileName(file)}");
            }

            Console.WriteLine($"Bundling completed. Output file created at: {output.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    },
    languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Enter the values for the following options:");


    string language;
    var validLanguages = new[] { "c#", "java", "js", "all" }; // רשימת ערכים חוקיים

    do
    {
        Console.Write("Language (c#, java, js, or all): ");
        language = Console.ReadLine()?.Trim().ToLower(); // הופך לאותיות קטנות כדי לתמוך במשתמשים שמקלידים בגדולות
        if (!validLanguages.Contains(language))
        {
            Console.WriteLine("Invalid input. Please enter one of the following options: c#, java, js, or all.");
            language = null; // מנקה את הקלט כדי להבטיח שהלולאה תמשיך
        }
    } while (string.IsNullOrEmpty(language));

    Console.WriteLine($"You selected: {language}");

    Console.Write("Output file path (e.g., output.txt): ");
    var output = Console.ReadLine()?.Trim() ?? "bundle.txt";

    Console.Write("Include note with file paths? (yes/no): ");
    var note = Console.ReadLine()?.Trim().ToLower() == "yes";

    Console.Write("Sort files by (name/type): ");
    var sort = Console.ReadLine()?.Trim() ?? "name";

    Console.Write("Remove empty lines? (yes/no): ");
    var removeEmptyLines = Console.ReadLine()?.Trim().ToLower() == "yes";

    Console.Write("Author name (optional): ");
    var author = Console.ReadLine()?.Trim();

    var command = $"bundle --language \"{language}\" --output \"{output}\"" +
              (note ? " --note" : "") +
              $" --sort \"{sort}\"" +
              (removeEmptyLines ? " --remove-empty-lines" : "") +
              (!string.IsNullOrWhiteSpace(author) ? $" --author \"{author}\"" : "");

    var rspFileName = "bundle.rsp";
    File.WriteAllText(rspFileName, command);

    Console.WriteLine($"Response file created: {rspFileName}");
    Console.WriteLine($"To run the command, use: dotnet @bundle.rsp");

});

var rootCommand = new RootCommand
{
    bundleCommand,
    createRspCommand
};

await rootCommand.InvokeAsync(args);
