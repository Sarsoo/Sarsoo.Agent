using Microsoft.Extensions.AI;
using Sarsoo.Agent.Content;
using Sarsoo.Agent.Extensions;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public interface IAgentMessage
{
    ContentType? ContentType { get; }
    
    string? AgentName { get; set; }

    void SetContent(object content);
}

public interface IAgentMessage<T>: IAgentMessage where T: AIContent
{
    T Content { get; }
    void SetContent(T content);
}

public class AgentMessage: BaseMessage, IAgentMessage
{
    public AgentMessage() : base()
    {
        Title = "";
        SchemeName = "AgentMessage";
    }

    public string? AgentName
    {
        get;
        set
        {
            field = value;
            if (string.IsNullOrWhiteSpace(value))
            {
                Title = "Agent";
            }
            else
            {
                Title = value;
            }
        }
    }

    public virtual ContentType? ContentType => null;

    public virtual void SetContent(object content) { }
}

public class AgentMessage<T>: AgentMessage, IAgentMessage<T> where T: AIContent
{
    private readonly Lock _textLock = new ();
    private string _accumulatedText = string.Empty;

    public T Content { get; protected set; }

    public AgentMessage(T content) : base()
    {
        Content = content;
        SetAccumulatedText(content.ToString() ?? "");
        Text = _accumulatedText;
        Title = Content.ContentType.ToString() ?? "";
    }

    public override ContentType? ContentType => Content?.ContentType;

    /// <summary>The text accumulated from the content so far. Safe to read from any thread.</summary>
    protected string AccumulatedText
    {
        get
        {
            lock (_textLock)
            {
                return _accumulatedText;
            }
        }
    }

    /// <summary>Replaces the accumulated text, e.g. when the first content chunk is seeded by a
    /// constructor that does not use <see cref="AIContent.ToString"/> as the initial text.</summary>
    protected void SetAccumulatedText (string text)
    {
        lock (_textLock)
        {
            _accumulatedText = text;
        }
    }

    /// <summary>Appends a streamed chunk to the accumulator without touching the view. Safe to call
    /// from any thread; schedule a redraw with <see cref="BaseMessage.RequestContentUpdate"/>.</summary>
    protected void AppendText (string? text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return;
        }

        lock (_textLock)
        {
            _accumulatedText += text;
        }
    }

    public virtual void SetContent(T content)
    {
        Content = content;
        AppendText(content?.ToString());
        RequestContentUpdate();
    }

    protected override void ApplyContentUpdate ()
    {
        Text = AccumulatedText;
        SetNeedsDraw ();
    }

    public override void SetContent(object content)
    {
        if (content is T typedContent)
        {
            // Redraw is requested by BaseMessage once the coalesced update is applied, so there is
            // no per-chunk event here.
            SetContent(typedContent);
        }
        else
        {
            throw new ArgumentException("Invalid content type");
        }
    }
}