// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_UNO && WINUI3
#nullable enable
using ColorCode.Common;
using ColorCode.Parsing;
using ColorCode.Styling;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace ColorCode;

/// <summary>
/// Port of ColorCode.WinUI's RichTextBlockFormatter, which only ships for Windows.
/// See https://github.com/CommunityToolkit/ColorCode-Universal/blob/main/ColorCode.UWP/RichTextBlockFormatter.cs
/// </summary>
public class RichTextBlockFormatter : CodeColorizerBase
{
    private InlineCollection? _inlines;

    public RichTextBlockFormatter(ElementTheme theme)
        : base(theme == ElementTheme.Dark ? StyleDictionary.DefaultDark : StyleDictionary.DefaultLight, null)
    {
    }

    public void FormatRichTextBlock(string sourceCode, ILanguage language, RichTextBlock richText)
    {
        var paragraph = new Paragraph();
        richText.Blocks.Add(paragraph);

        _inlines = paragraph.Inlines;
        languageParser.Parse(sourceCode, language, Write);
    }

    protected override void Write(string parsedSourceCode, IList<Scope> scopes)
    {
        List<TextInsertion> styleInsertions = new();

        foreach (var scope in scopes)
        {
            GetStyleInsertionsForCapturedStyle(scope, styleInsertions);
        }

        styleInsertions.SortStable((x, y) => x.Index.CompareTo(y.Index));

        int offset = 0;
        Scope? previousScope = null;

        foreach (var insertion in styleInsertions)
        {
            AddRun(parsedSourceCode.Substring(offset, insertion.Index - offset), previousScope);
            offset = insertion.Index;
            previousScope = insertion.Scope;
        }

        var remaining = parsedSourceCode.Substring(offset);

        // Ensures that those loose carriages don't run away!
        if (remaining != "\r")
        {
            AddRun(remaining, null);
        }
    }

    private void AddRun(string text, Scope? scope)
    {
        Run run = new() { Text = text };

        if (scope is not null && Styles.Contains(scope.Name))
        {
            var style = Styles[scope.Name];

            if (!string.IsNullOrWhiteSpace(style.Foreground))
            {
                run.Foreground = new SolidColorBrush(ParseColor(style.Foreground));
            }

            if (style.Italic)
            {
                run.FontStyle = FontStyle.Italic;
            }

            if (style.Bold)
            {
                run.FontWeight = FontWeights.Bold;
            }
        }

        _inlines!.Add(run);
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.Replace("#", string.Empty);

        byte a = 255;
        int index = 0;

        if (hex.Length == 8)
        {
            a = Convert.ToByte(hex.Substring(index, 2), 16);
            index += 2;
        }

        byte r = Convert.ToByte(hex.Substring(index, 2), 16);
        byte g = Convert.ToByte(hex.Substring(index + 2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(index + 4, 2), 16);

        return Windows.UI.Color.FromArgb(a, r, g, b);
    }

    private static void GetStyleInsertionsForCapturedStyle(Scope scope, ICollection<TextInsertion> styleInsertions)
    {
        styleInsertions.Add(new TextInsertion { Index = scope.Index, Scope = scope });

        foreach (var childScope in scope.Children)
        {
            GetStyleInsertionsForCapturedStyle(childScope, styleInsertions);
        }

        styleInsertions.Add(new TextInsertion { Index = scope.Index + scope.Length });
    }
}
#endif
