using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DJ.Helper
{
    /// <summary>
    /// Attached behavior to render a TextBlock with highlighted segments based on ActiveSearchTerms.
    /// It uses the ancestor NLogViewer to access resolvers and highlight brush.
    /// </summary>
    public static class MessageHighlighter
    {
        /// <summary>
        /// Which text source to highlight: resolved Message or LoggerName
        /// </summary>
        public enum HighlightSource
        {
            Message,
            LoggerName
        }

        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(MessageHighlighter),
            new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source",
            typeof(HighlightSource),
            typeof(MessageHighlighter),
            new PropertyMetadata(HighlightSource.Message, OnSourceChanged));

        public static void SetSource(DependencyObject element, HighlightSource value) => element.SetValue(SourceProperty, value);
        public static HighlightSource GetSource(DependencyObject element) => (HighlightSource)element.GetValue(SourceProperty);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                if ((bool)e.NewValue)
                {
                    textBlock.Loaded += TextBlockOnLoaded;
                    textBlock.Unloaded += TextBlockOnUnloaded;
                    TryUpdate(textBlock);
                }
                else
                {
                    textBlock.Loaded -= TextBlockOnLoaded;
                    textBlock.Unloaded -= TextBlockOnUnloaded;
                }
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb && GetEnable(tb))
            {
                TryUpdate(tb);
            }
        }

        private static void TextBlockOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                TryUpdate(tb);
        }

        private static void TextBlockOnUnloaded(object sender, RoutedEventArgs e)
        {
            // no-op
        }

        /// <summary>
        /// Rebuilds the Inlines of the TextBlock, highlighting matches for any ActiveSearchTerms.
        /// </summary>
        private static void TryUpdate(TextBlock textBlock)
        {
            var nlogViewer = FindAncestor<NLogViewer>(textBlock);
            if (nlogViewer == null)
                return;

            var logEventInfo = textBlock.DataContext as NLog.LogEventInfo;
            if (logEventInfo == null)
                return;

            string message = GetSource(textBlock) == HighlightSource.LoggerName
                ? (nlogViewer.LoggerNameResolver?.Resolve(logEventInfo) ?? string.Empty)
                : (nlogViewer.MessageResolver?.Resolve(logEventInfo) ?? string.Empty);
            var terms = nlogViewer.ActiveSearchTerms;
            if (string.IsNullOrEmpty(message) || terms == null || terms.Count == 0)
            {
                textBlock.Text = message; // fallback
                return;
            }

            // Build match ranges for all terms (OR). Merge overlapping.
            var ranges = new List<(int start, int length)>();
            foreach (var term in terms)
            {
                foreach (var r in FindMatches(message, term))
                    ranges.Add(r);
            }
            if (ranges.Count == 0)
            {
                textBlock.Text = message;
                return;
            }

            ranges.Sort((a,b) => a.start.CompareTo(b.start));
            var merged = new List<(int start, int length)>();
            foreach (var r in ranges)
            {
                if (merged.Count == 0)
                {
                    merged.Add(r);
                }
                else
                {
                    var last = merged[merged.Count - 1];
                    if (r.start <= last.start + last.length)
                    {
                        int end = Math.Max(last.start + last.length, r.start + r.length);
                        merged[merged.Count - 1] = (last.start, end - last.start);
                    }
                    else
                    {
                        merged.Add(r);
                    }
                }
            }

            // Rebuild inlines
            textBlock.Inlines.Clear();
            int index = 0;
            foreach (var (start, length) in merged)
            {
                if (start > index)
                {
                    textBlock.Inlines.Add(new Run(message.Substring(index, start - index)));
                }
                var run = new Run(message.Substring(start, length))
                {
                    Background = nlogViewer.SearchHighlightBackground
                };
                textBlock.Inlines.Add(run);
                index = start + length;
            }
            if (index < message.Length)
            {
                textBlock.Inlines.Add(new Run(message.Substring(index)));
            }

            // store raw text in tag property
            textBlock.Tag = new TextRange(textBlock.ContentStart, textBlock.ContentEnd).Text;
        }

        private static IEnumerable<(int start, int length)> FindMatches(string text, SearchTerm term)
        {
            if (term is RegexSearchTerm regexTerm)
            {
                foreach (Match m in Regex.Matches(text, regexTerm.Text))
                {
                    if (m.Success && m.Length > 0)
                        yield return (m.Index, m.Length);
                }
            }
            else if (term is TextSearchTerm simple)
            {
                string needle = simple.ToString();
                if (string.IsNullOrEmpty(needle)) yield break;
                int pos = 0;
                var comparison = StringComparison.InvariantCultureIgnoreCase;
                while ((pos = text.IndexOf(needle, pos, comparison)) >= 0)
                {
                    yield return (pos, needle.Length);
                    pos += needle.Length;
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T typed) return typed;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}


