using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using termix.models;
using SharpCompress.Archives;
using SharpCompress.Common;
using termix.Helpers;

namespace termix.Services;

public class FilePreviewService(IconProvider iconProvider)
{
    private readonly CustomSyntaxHighlighter _highlighter = new();
    private const string TabReplacement = "    ";

    public IRenderable GetPreview(string? filePath, int verticalOffset, int horizontalOffset, bool showHidden)
    {
        string header;

        if (string.IsNullOrEmpty(filePath) || (!File.Exists(filePath) && !Directory.Exists(filePath)))
            return new Panel(Align.Center(new Text("Select a file to preview"), VerticalAlignment.Middle))
                .Expand().Border(BoxBorder.Rounded).Header("[grey]Preview[/]");

        if (Directory.Exists(filePath))
        {
            var directoryInfo = new DirectoryInfo(filePath);
            header = $"[white]Preview (Directory):[/] [aqua]{directoryInfo.Name.EscapeMarkup()}[/]";
            return RenderDirectoryPreview(filePath, header, showHidden);
        }

        var fileInfo = new FileInfo(filePath);
        header = $"[white]Preview (Scroll: Alt+Arrows):[/] [aqua]{fileInfo.Name.EscapeMarkup()}[/]";

        try
        {
            if (FileTypeHelper.IsArchiveFile(fileInfo.Extension))
            {
                return RenderArchivePreview(filePath, header);
            }

            if (FileTypeHelper.IsImageFile(fileInfo.Extension))
            {
                return RenderImagePreview(filePath, header);
            }

            using var fileStream = File.OpenRead(filePath);
            if (FileTypeHelper.IsBinary(fileStream))
                return new Panel(Align.Center(new Text("Binary File\nNo preview available"), VerticalAlignment.Middle))
                    .Header(header).Expand().Border(BoxBorder.Rounded);

            var textPreviewHeight = Console.WindowHeight - 12;
            var allLines = File.ReadAllLines(filePath, Encoding.UTF8);

            var visibleLines = allLines
                .Skip(verticalOffset)
                .Take(textPreviewHeight)
                .Select(line =>
                    line.Length > horizontalOffset ? line[horizontalOffset..].Replace("\t", TabReplacement) : "")
                .ToArray();

            IRenderable content = visibleLines.Length == 0
                ? Align.Center(new Text("[grey]-- End of File --[/]"), VerticalAlignment.Middle)
                : _highlighter.Highlight(string.Join("\n", visibleLines), fileInfo.Extension);

            return new Panel(content).Header(header).Expand().Border(BoxBorder.Rounded);
        }
        catch (Exception ex)
        {
            var errorMessage = ex is UnknownImageFormatException
                ? "[red]Cannot render image: Unsupported or corrupt file.[/]"
                : $"[red]Error reading file:[/] {ex.Message.EscapeMarkup()}";

            return new Panel(Align.Center(new Text(errorMessage), VerticalAlignment.Middle))
                .Header(header).Expand().Border(BoxBorder.Rounded);
        }
    }

    private static IRenderable RenderImagePreview(string filePath, string header)
    {
        using var image = Image.Load<Rgba32>(filePath);

        const int horizontalFactor = 3;
        const int verticalFactor = 3;

        var consoleWidth = (Console.WindowWidth / 2) - 10;
        var consoleHeight = Console.WindowHeight - 12;

        var targetWidth = consoleWidth * horizontalFactor;
        var targetHeight = consoleHeight * 2 * verticalFactor;

        image.Mutate(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(targetWidth, targetHeight),
                Sampler = KnownResamplers.Lanczos3
            });
            ctx.Dither(KnownDitherings.FloydSteinberg);
        });

        var sb = new StringBuilder();
        const int sampleArea = horizontalFactor * verticalFactor;

        for (var y = 0; y < image.Height - (verticalFactor * 2 - 1); y += verticalFactor * 2)
        {
            for (var x = 0; x < image.Width - (horizontalFactor - 1); x += horizontalFactor)
            {
                long topR = 0, topG = 0, topB = 0;
                long botR = 0, botG = 0, botB = 0;

                for (var offsetY = 0; offsetY < verticalFactor; offsetY++)
                {
                    for (var offsetX = 0; offsetX < horizontalFactor; offsetX++)
                    {
                        var pTop = image[x + offsetX, y + offsetY];
                        topR += pTop.R;
                        topG += pTop.G;
                        topB += pTop.B;

                        var pBot = image[x + offsetX, y + offsetY + verticalFactor];
                        botR += pBot.R;
                        botG += pBot.G;
                        botB += pBot.B;
                    }
                }

                var upperColor = new Rgba32((byte)(topR / sampleArea), (byte)(topG / sampleArea),
                    (byte)(topB / sampleArea));
                var lowerColor = new Rgba32((byte)(botR / sampleArea), (byte)(botG / sampleArea),
                    (byte)(botB / sampleArea));

                sb.Append(
                    $"[rgb({lowerColor.R},{lowerColor.G},{lowerColor.B}) on rgb({upperColor.R},{upperColor.G},{upperColor.B})]▄[/]");
            }

            sb.AppendLine();
        }

        return new Panel(Align.Center(new Markup(sb.ToString()), VerticalAlignment.Middle))
            .Header(header)
            .Expand()
            .Border(BoxBorder.Rounded);
    }

    private IRenderable RenderDirectoryPreview(string directoryPath, string header, bool showHidden)
    {
        try
        {
            var items = FileSystemService
                .GetDirectoryContents(directoryPath, SortBy.Name, SortDirection.Ascending, true, showHidden)
                .Where(item => !item.IsParentDirectory)
                .Take(Console.WindowHeight - 14)
                .ToList();

            if (items.Count == 0)
            {
                return new Panel(Align.Center(new Text("[grey]-- Empty Directory --[/]"), VerticalAlignment.Middle))
                    .Header(header).Expand().Border(BoxBorder.Rounded);
            }

            var tree = new Tree($"[aqua bold]{new DirectoryInfo(directoryPath).Name.EscapeMarkup()}[/]");
            foreach (var nodeText in from item in items
                     let icon = iconProvider.GetIcon(item)
                     select $"{icon} {item.Name.EscapeMarkup()}")
            {
                tree.AddNode(nodeText);
            }

            return new Panel(tree).Header(header).Expand().Border(BoxBorder.Rounded);
        }
        catch (Exception ex)
        {
            var errorMessage = $"[red]Error reading directory:[/] {ex.Message.EscapeMarkup()}";
            return new Panel(Align.Center(new Text(errorMessage), VerticalAlignment.Middle))
                .Header(header).Expand().Border(BoxBorder.Rounded);
        }
    }

    private IRenderable RenderArchivePreview(string filePath, string header)
    {
        try
        {
            using var archive = ArchiveFactory.Open(filePath);
            var allEntries = archive.Entries
                .Where(e => !e.IsDirectory)
                .OrderBy(e => e.Key)
                .ToList();

            if (allEntries.Count == 0)
            {
                return new Panel(Align.Center(new Text("[grey]-- Archive is empty --[/]"), VerticalAlignment.Middle))
                    .Header(header).Expand().Border(BoxBorder.Rounded);
            }

            var tree = new Tree($"[aqua bold]{new FileInfo(filePath).Name.EscapeMarkup()}[/]")
            {
                Guide = TreeGuide.Line
            };

            var directoryNodes = new Dictionary<string, IHasTreeNodes>();
            var processedItems = 0;

            foreach (var entry in allEntries)
            {
                if (processedItems >= Console.WindowHeight - 14) break;

                var entryPath = (entry.Key ?? string.Empty).Replace('\\', '/');
                var pathParts = entryPath.Split('/');

                IHasTreeNodes currentNode = tree;

                for (var i = 0; i < pathParts.Length - 1; i++)
                {
                    var dirName = pathParts[i];
                    var currentPath = string.Join("/", pathParts.Take(i + 1));

                    if (!directoryNodes.TryGetValue(currentPath, out var node))
                    {
                        var dirItem = new FileSystemItem(
                            Path: currentPath,
                            Name: dirName,
                            IsDirectory: true,
                            Size: 0,
                            LastModified: DateTime.MinValue,
                            IsParentDirectory: false
                        );
                        var icon = iconProvider.GetIcon(dirItem);
                        var newNode = currentNode.AddNode($"{icon} {dirName.EscapeMarkup()}");
                        directoryNodes[currentPath] = newNode;
                        currentNode = newNode;
                    }
                    else
                    {
                        currentNode = node;
                    }
                }

                var fileName = pathParts.Last();
                if (string.IsNullOrEmpty(fileName)) continue;

                var fileItem = new FileSystemItem(
                    Path: entryPath,
                    Name: fileName,
                    IsDirectory: false,
                    Size: entry.Size,
                    LastModified: entry.LastModifiedTime?.Date ?? DateTime.MinValue,
                    IsParentDirectory: false
                );

                var iconValue = iconProvider.GetIcon(fileItem);
                var nodeText = $"{iconValue} {fileName.EscapeMarkup()} [yellow]({fileItem.FormattedSize})[/]";
                currentNode.AddNode(nodeText);

                processedItems++;
            }

            return new Panel(tree).Header(header).Expand().Border(BoxBorder.Rounded);
        }
        catch (InvalidFormatException ex)
        {
            var errorMessage =
                $"[red]Cannot preview archive:[/] The file may be corrupt or an unsupported format.\n[grey]{ex.Message.EscapeMarkup()}[/]";
            return new Panel(Align.Center(new Text(errorMessage), VerticalAlignment.Middle))
                .Header(header).Expand().Border(BoxBorder.Rounded);
        }
        catch (Exception ex)
        {
            var errorMessage = $"[red]Error reading archive:[/] {ex.Message.EscapeMarkup()}";
            return new Panel(Align.Center(new Text(errorMessage), VerticalAlignment.Middle))
                .Header(header).Expand().Border(BoxBorder.Rounded);
        }
    }
}