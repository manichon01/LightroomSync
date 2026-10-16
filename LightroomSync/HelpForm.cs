using System.Reflection;
using System.Text.RegularExpressions;

namespace LightroomSync
{
    // Shows the embedded Help.md: a topic list on the left and the guide on the right.
    // Only the small Markdown subset used by Help.md is understood: # / ## / ### headings, "- " bullets,
    // "1. " numbered items, "> " notes, **bold** and `code`.
    internal class HelpForm : Form
    {
        private readonly RichTextBox text = new();
        private readonly ListBox topics = new();
        private readonly List<int> topicPositions = new();

        public HelpForm(Font baseFont, Icon? icon)
        {
            Text = "Lightroom Sync Help";
            Font = baseFont;
            Icon = icon;
            ClientSize = new Size(980, 680);
            MinimumSize = new Size(640, 400);
            StartPosition = FormStartPosition.CenterParent;

            topics.Dock = DockStyle.Left;
            topics.Width = 300;
            topics.IntegralHeight = false;
            topics.BorderStyle = BorderStyle.None;
            topics.SelectedIndexChanged += (_, _) => JumpToTopic();

            text.Dock = DockStyle.Fill;
            text.ReadOnly = true;
            text.BorderStyle = BorderStyle.None;
            text.BackColor = SystemColors.Window;
            text.ForeColor = SystemColors.WindowText;
            text.DetectUrls = true;
            text.LinkClicked += (_, e) => Utils.OpenURL(e.LinkText!);

            // Added fill first, then the docked list, so the list takes its width and the text gets the rest.
            Controls.Add(text);
            Controls.Add(topics);

            Render(LoadMarkdown());
            text.SelectionStart = 0;
        }

        private static string LoadMarkdown()
        {
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LightroomSync.Help.md");
            if (stream == null)
            {
                return "# Help\n\nThe help document is missing from this build.";
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private void JumpToTopic()
        {
            int index = topics.SelectedIndex;
            if (index < 0 || index >= topicPositions.Count)
            {
                return;
            }
            // Scroll to the end first so the chosen heading ends up at the top of the view.
            text.SelectionStart = text.TextLength;
            text.ScrollToCaret();
            text.SelectionStart = topicPositions[index];
            text.ScrollToCaret();
        }

        private void Render(string markdown)
        {
            Font body = Font;
            foreach (string raw in markdown.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0)
                {
                    Append("\n", body, 0, 0);
                }
                else if (line.StartsWith("# "))
                {
                    Append(line.Substring(2) + "\n\n", new Font(body.FontFamily, body.Size + 8, FontStyle.Bold), 0, 0);
                }
                else if (line.StartsWith("## "))
                {
                    topicPositions.Add(text.TextLength);
                    topics.Items.Add(line.Substring(3));
                    Append(line.Substring(3) + "\n", new Font(body.FontFamily, body.Size + 4, FontStyle.Bold), 0, 0);
                }
                else if (line.StartsWith("### "))
                {
                    Append(line.Substring(4) + "\n", new Font(body.FontFamily, body.Size + 1, FontStyle.Bold), 0, 0);
                }
                else if (line.StartsWith("- "))
                {
                    Append("•  " + line.Substring(2) + "\n", body, 18, 14);
                }
                else if (Regex.IsMatch(line, @"^\d+\. "))
                {
                    Append(line + "\n", body, 18, 18);
                }
                else if (line.StartsWith("> "))
                {
                    Append(line.Substring(2) + "\n", new Font(body, FontStyle.Italic), 18, 0);
                }
                else
                {
                    Append(line + "\n", body, 0, 0);
                }
            }
        }

        // Writes one paragraph at the end, turning **bold** and `code` spans into font changes.
        private void Append(string paragraph, Font font, int indent, int hanging)
        {
            text.SelectionStart = text.TextLength;
            text.SelectionLength = 0;
            text.SelectionIndent = indent;
            text.SelectionHangingIndent = hanging;

            foreach (string part in Regex.Split(paragraph, @"(\*\*.+?\*\*|`.+?`)"))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                Font partFont = font;
                string shown = part;
                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    partFont = new Font(font, font.Style | FontStyle.Bold);
                    shown = part.Substring(2, part.Length - 4);
                }
                else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
                {
                    partFont = new Font("Consolas", font.Size);
                    shown = part.Substring(1, part.Length - 2);
                }

                text.SelectionStart = text.TextLength;
                text.SelectionLength = 0;
                text.SelectionFont = partFont;
                text.SelectedText = shown;
            }
        }
    }
}
