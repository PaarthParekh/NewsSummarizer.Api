using System.Text;

namespace NewsSummarizer.Api.Services;

public static class HtmlUtils
{
    public static string StripTags(string html)
    {
        var inTag = false;
        var sb = new StringBuilder(html.Length);
        foreach (var ch in html)
        {
            if (ch == '<') inTag = true;
            else if (ch == '>') inTag = false;
            else if (!inTag) sb.Append(ch);
        }
        return sb.ToString();
    }
}