using System.Globalization;
using System.Reflection;
using System.Resources;

namespace WhatPicturePath;

internal static class LocalizationHelper
{
    private static readonly ResourceManager ResourceManager = new("WhatPicturePath.Resources", Assembly.GetExecutingAssembly());

    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    public static string GetString(string name) => ResourceManager.GetString(name, CurrentCulture) ?? string.Empty;

    public static string GetString(string name, params object[] args) => string.Format(CurrentCulture, GetString(name), args);

    public static void Initialize()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            CurrentCulture = new CultureInfo("zh-CN");
        }
        else
        {
            CurrentCulture = new CultureInfo("en-US");
        }
    }
}
