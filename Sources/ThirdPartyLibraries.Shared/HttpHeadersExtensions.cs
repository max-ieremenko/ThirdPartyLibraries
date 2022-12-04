using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;

namespace ThirdPartyLibraries.Shared;

public static class HttpHeadersExtensions
{
    public static bool TryGetValue(this HttpHeaders headers, string name, out string value)
    {
        value = null;
        if (!headers.TryGetValues(name, out var values))
        {
            return false;
        }

        value = values.FirstOrDefault();
        return !string.IsNullOrEmpty(value);
    }

    public static bool TryGetInt64Value(this HttpHeaders headers, string name, out long value)
    {
        value = 0;
        if (!TryGetValue(headers, name, out var text))
        {
            return false;
        }

        return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}