using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WhatPicturePath;

internal static class Program
{
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
        LocalizationHelper.Initialize();
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
                        ShowError(LocalizationHelper.GetString("ErrorNoFilesSelected"));
                        TryReadKey();
                    }
                    break;

                default:
                    ShowError(LocalizationHelper.GetString("ErrorInvalidOption"));
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
        Console.WriteLine($"=== {LocalizationHelper.GetString("TitleMain")} ===");
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("MenuSelectedCount", selectedCount));
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("MenuOption1"));
        Console.WriteLine(LocalizationHelper.GetString("MenuOption2"));
        Console.WriteLine(LocalizationHelper.GetString("MenuOption3"));
        Console.WriteLine();
        Console.Write(LocalizationHelper.GetString("MenuPrompt"));
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
                    LocalizationHelper.GetString(
                        "AddSuccess",
                        addedCount,
                        openFileDialog.FileNames.Length - addedCount
                    )
                );
            }
            else
            {
                ShowError(LocalizationHelper.GetString("AddNoNew"));
            }
        }
        else
        {
            ShowInfo(LocalizationHelper.GetString("AddNoSelection"));
        }
    }

    private static void SelectFilesViaPathPaste(List<string> selectedFiles)
    {
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("PasteHint"));
        Console.WriteLine(LocalizationHelper.GetString("PasteQuoteHint"));
        Console.WriteLine();
        Console.Write(LocalizationHelper.GetString("PromptFilePath"));
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowInfo(LocalizationHelper.GetString("PasteNoInput"));
            TryReadKey();
            return;
        }

        var filePaths = ParseFilePaths(input);
        var addedCount = AddFilesToList(selectedFiles, filePaths);

        if (addedCount > 0)
        {
            ShowSuccess(
                LocalizationHelper.GetString(
                    "PasteAddSuccess",
                    addedCount,
                    filePaths.Length - addedCount
                )
            );
        }
        else
        {
            ShowError(LocalizationHelper.GetString("AddNoNew"));
        }
    }

    private static string[] ParseFilePaths(string input)
    {
        var paths = new List<string>();
        var currentPath = new StringBuilder();
        var inQuotes = false;
        var escapeNext = false;

        foreach (char c in input)
        {
            if (escapeNext)
            {
                currentPath.Append(c);
                escapeNext = false;
            }
            else if (c == '\\')
            {
                escapeNext = true;
            }
            else if (c == '"' || c == '\'')
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
        Console.WriteLine($"=== {LocalizationHelper.GetString("OutputTitle")} ===");
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("OutputTotalCount", selectedFiles.Count));
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("OutputFormatOption1"));
        Console.WriteLine(LocalizationHelper.GetString("OutputFormatOption2"));
        Console.WriteLine();
        Console.Write(LocalizationHelper.GetString("OutputFormatPrompt"));

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
        Console.WriteLine(LocalizationHelper.GetString("ExitPrompt"));
        TryReadKey();
    }

    private static void OutputWithNewlines(List<string> selectedFiles)
    {
        foreach (var filePath in selectedFiles)
        {
            if (TryConvertToFileProtocolPath(filePath, out var protocolPath))
            {
                Console.WriteLine(protocolPath);
            }
        }
    }

    private static void OutputWithCommas(List<string> selectedFiles)
    {
        var validPaths = selectedFiles
            .Where(filePath => TryConvertToFileProtocolPath(filePath, out _))
            .ToList();
        Console.WriteLine(
            string.Join(
                ", ",
                validPaths.Select(filePath =>
                    TryConvertToFileProtocolPath(filePath, out var protocolPath)
                        ? protocolPath
                        : string.Empty
                )
            )
        );
    }

    private static bool TryConvertToFileProtocolPath(string filePath, out string protocolPath)
    {
        try
        {
            protocolPath = new Uri(filePath).AbsoluteUri;
            return true;
        }
        catch
        {
            protocolPath = string.Empty;
            return false;
        }
    }

    private static OpenFileDialog CreateOpenFileDialog()
    {
        return new OpenFileDialog
        {
            Title = LocalizationHelper.GetString("DialogTitle"),
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
        Console.WriteLine(LocalizationHelper.GetString("Success", message));
        Console.ResetColor();
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("Error", message));
        Console.ResetColor();
    }

    private static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(LocalizationHelper.GetString("Info", message));
        Console.ResetColor();
    }
}
