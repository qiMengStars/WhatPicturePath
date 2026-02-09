using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WhatPicturePath;

internal static class Program
{
    private const string DialogTitle = "请选择图片文件";
    private const string Filter =
        "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.webp|所有文件|*.*";

    private static readonly string[] SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".tiff",
        ".webp",
    ];

    [STAThread]
    private static void Main()
    {
        var selectedFiles = new List<string>();

        while (true)
        {
            TryClearConsole();
            ShowMainMenu(selectedFiles.Count);

            var choice = TryReadKey();

            switch (choice)
            {
                case '1':
                    SelectFilesViaWindowsDialog(selectedFiles);
                    break;

                case '2':
                    SelectFilesViaPathPaste(selectedFiles);
                    break;

                case '3':
                    if (selectedFiles.Count > 0)
                    {
                        OutputAllFileProtocolPaths(selectedFiles);
                        return;
                    }
                    else
                    {
                        ShowError("当前没有选择任何文件，请先选择文件。");
                        TryReadKey();
                    }
                    break;

                default:
                    ShowError("无效选项，请重新选择。");
                    TryReadKey();
                    break;
            }
        }
    }

    private static char TryReadKey()
    {
        try
        {
            if (Console.IsInputRedirected)
            {
                // 如果输入被重定向，读取一行并取第一个字符
                var line = Console.ReadLine();
                return string.IsNullOrEmpty(line) ? '\0' : line[0];
            }
            return Console.ReadKey(true).KeyChar;
        }
        catch
        {
            // 如果控制台不可用，返回默认值
            return '\0';
        }
    }

    private static void TryClearConsole()
    {
        try
        {
            if (Console.IsOutputRedirected || Console.IsErrorRedirected)
            {
                Console.WriteLine(new string('=', 50));
                return;
            }
            Console.Clear();
        }
        catch
        {
            // 如果控制台不可用，忽略清除操作
            Console.WriteLine(new string('=', 50));
        }
    }

    private static void ShowMainMenu(int selectedCount)
    {
        Console.WriteLine("=== 图片文件路径获取工具 ===");
        Console.WriteLine();
        Console.WriteLine($"当前已选择 {selectedCount} 个文件");
        Console.WriteLine();
        Console.WriteLine("请选择操作：");
        Console.WriteLine("1. 通过 Windows 文件选择器添加文件");
        Console.WriteLine("2. 通过粘贴文件路径添加文件");
        Console.WriteLine("3. 输出所有 File 协议路径并退出");
        Console.WriteLine();
        Console.Write("请输入选项 (1-3): ");
    }

    private static void SelectFilesViaWindowsDialog(List<string> selectedFiles)
    {
        using var openFileDialog = CreateOpenFileDialog();

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            var addedCount = AddFilesToList(selectedFiles, openFileDialog.FileNames);

            if (addedCount > 0)
            {
                ShowSuccess(
                    $"成功添加 {addedCount} 个文件（跳过了 {openFileDialog.FileNames.Length - addedCount} 个重复或无效文件）"
                );
            }
            else
            {
                ShowError("没有添加任何新文件（所有文件都已存在或格式不支持）");
            }
        }
        else
        {
            ShowInfo("未选择任何文件");
        }
    }

    private static void SelectFilesViaPathPaste(List<string> selectedFiles)
    {
        Console.WriteLine();
        Console.WriteLine("请粘贴文件路径（多个文件用空格或分号分隔）：");
        Console.WriteLine("提示：可以用引号包裹包含空格的路径");
        Console.WriteLine();
        Console.Write("文件路径: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowInfo("未输入文件路径");
            TryReadKey();
            return;
        }

        var filePaths = ParseFilePaths(input);
        var addedCount = AddFilesToList(selectedFiles, filePaths);

        if (addedCount > 0)
        {
            ShowSuccess(
                $"成功添加 {addedCount} 个文件（跳过了 {filePaths.Length - addedCount} 个重复、不存在或格式不支持的文件）"
            );
        }
        else
        {
            ShowError("没有添加任何新文件（所有文件都已存在、不存在或格式不支持）");
        }
    }

    private static string[] ParseFilePaths(string input)
    {
        var paths = new List<string>();
        var currentPath = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"' || c == '\'')
            {
                inQuotes = !inQuotes;
            }
            else if ((c == ' ' || c == ';') && !inQuotes)
            {
                if (currentPath.Length > 0)
                {
                    paths.Add(currentPath.ToString().Trim());
                    currentPath.Clear();
                }
            }
            else
            {
                currentPath.Append(c);
            }
        }

        if (currentPath.Length > 0)
        {
            paths.Add(currentPath.ToString().Trim());
        }

        return [.. paths];
    }

    private static int AddFilesToList(List<string> selectedFiles, string[] filePaths)
    {
        var validNewFiles = filePaths
            .Where(filePath =>
                IsFileValid(filePath)
                && !selectedFiles.Contains(filePath, StringComparer.OrdinalIgnoreCase)
            )
            .ToList();

        selectedFiles.AddRange(validNewFiles);
        return validNewFiles.Count;
    }

    private static bool IsFileValid(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return Array.Exists(
            SupportedExtensions,
            ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static void OutputAllFileProtocolPaths(List<string> selectedFiles)
    {
        TryClearConsole();
        Console.WriteLine("=== File 协议路径 ===");
        Console.WriteLine();
        Console.WriteLine($"共 {selectedFiles.Count} 个文件");
        Console.WriteLine();
        Console.WriteLine("请选择输出格式：");
        Console.WriteLine("1. 换行分隔");
        Console.WriteLine("2. 逗号分隔（数组形式）");
        Console.WriteLine();
        Console.Write("请输入选项 (1-2): ");

        var choice = TryReadKey();

        Console.WriteLine();
        Console.WriteLine();

        if (choice == '2')
        {
            OutputWithCommas(selectedFiles);
        }
        else
        {
            OutputWithNewlines(selectedFiles);
        }

        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        TryReadKey();
    }

    private static void OutputWithNewlines(List<string> selectedFiles)
    {
        foreach (var filePath in selectedFiles)
        {
            var fileProtocolPath = new Uri(filePath).AbsoluteUri;
            Console.WriteLine(fileProtocolPath);
        }
    }

    private static void OutputWithCommas(List<string> selectedFiles)
    {
        var paths = selectedFiles.Select(filePath => new Uri(filePath).AbsoluteUri);
        Console.WriteLine(string.Join(", ", paths));
    }

    private static OpenFileDialog CreateOpenFileDialog()
    {
        return new OpenFileDialog
        {
            Title = DialogTitle,
            Filter = Filter,
            Multiselect = true,
            CheckFileExists = true,
            CheckPathExists = true,
        };
    }

    private static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    private static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }
}
