using System;
using System.IO;

namespace LagerInsights.IO;

internal static class Bilder
{
    public static string readBase64(string path)
    {
        var imageArray = File.ReadAllBytes(path);
        return Convert.ToBase64String(imageArray);
    }
}