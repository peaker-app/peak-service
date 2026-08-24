using System.Net;

namespace PeakService.Infrastructure.ExternalServices;

internal static class CommonsFileTitle
{
    private const string FilePathSegment = "/wiki/Special:FilePath/";
    private const string FilePrefix = "File:";

    public static string? From(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? parsed))
        {
            return null;
        }

        int start = parsed.AbsolutePath.IndexOf(FilePathSegment, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        string encoded = parsed.AbsolutePath[(start + FilePathSegment.Length)..];

        return encoded.Length == 0 ? null : string.Concat(FilePrefix, WebUtility.UrlDecode(encoded));
    }
}
