using System.IO;

namespace CineBook.Models;

internal sealed record MovieCinematicMedia(
    string PrimaryColor, string SecondaryColor, string AccentColor, string? PreviewVideoUrl = null)
{
    // Điền URL trực tiếp của clip ngắn ở tham số cuối khi có video thật.
    private static readonly Dictionary<string, MovieCinematicMedia> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m01"] = new("#981D46", "#231D55", "#CCA066"),
        ["m02"] = new("#781528", "#32213F", "#797485"),
        ["m03"] = new("#316CB9", "#182A67", "#BED5E9"),
        ["m04"] = new("#BF668A", "#554081", "#DE895D"),
        ["m05"] = new("#BB541F", "#681C29", "#D6A44E"),
        ["m06"] = new("#486984", "#162C4D", "#A1B3C1"),
        ["m07"] = new("#2D8E8A", "#1A4875", "#D3AD62"),
        ["m08"] = new("#57647B", "#23365F", "#BF4554"),
        ["m09"] = new("#71539B", "#292548", "#AFA3DA"),
        ["m10"] = new("#AA6D31", "#63223F", "#D5AB70"),
        ["m11"] = new("#205D72", "#14294B", "#5D9C95"),
        ["m12"] = new("#AE4C36", "#322457", "#DFAB5A"),
    };

    public static MovieCinematicMedia For(Movie movie) =>
        Catalog.TryGetValue(movie.Id, out var media)
            ? media
            : new(movie.PlaceholderColorHex, "#182847", "#D4A84B");

    public static Uri? ResolveUri(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        if (!Uri.TryCreate(source.Trim(), UriKind.Absolute, out var uri)) return null;

        if (uri.IsFile) return File.Exists(uri.LocalPath) ? uri : null;
        return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp ? uri : null;
    }
}
