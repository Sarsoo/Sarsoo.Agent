using Microsoft.Extensions.AI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class TextMessage: AgentMessage<TextContent>
{
    private Markdown Markdown = new()
    {
        Text = "",
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        BorderStyle = LineStyle.None
    };
    
    public TextMessage(TextContent content) : base(content)
    {
        Title = "Agent";
        Text = string.Empty;
        BorderStyle = LineStyle.Single;

        Markdown.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
        Markdown.ContentSizeChanged += (sender, args) =>
        {
            Height = Markdown.GetContentHeight();
            SetNeedsDraw();
        };
        
        Add(Markdown);
    }
    
    private string _totalMessage = string.Empty;
    private readonly Lock _messageLock = new();
    public override void SetContent(TextContent content)
    {
        Content = content;

        var c = content.ToString();
        if (!string.IsNullOrEmpty(c))
        {
            string toSet;
            lock (_messageLock)
            {
                _totalMessage += c;
                toSet = _totalMessage;
            }
            App?.Invoke(() =>
                {
                    Markdown.Text = toSet;
                }
            );
        }
    }
}