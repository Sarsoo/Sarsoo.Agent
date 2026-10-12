using Microsoft.Extensions.AI;
using Sarsoo.Agent.Content;
using Sarsoo.Agent.Extensions;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public interface IAgentMessage
{
    ContentType? ContentType { get; }

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

    public virtual ContentType? ContentType => null;

    public virtual void SetContent(object content) { }
}

public class AgentMessage<T>: AgentMessage, IAgentMessage<T> where T: AIContent
{
    public T Content { get; protected set; }

    public AgentMessage(T content) : base()
    {
        Content = content;
        Text = content.ToString() ?? "";
        Title = Content.ContentType.ToString() ?? "";
    }

    public override ContentType? ContentType => Content?.ContentType;

    public virtual void SetContent(T content)
    {
        Content = content;

        if (!string.IsNullOrEmpty(content.ToString()))
        {
            App?.Invoke(() =>
            {
                Text += content?.ToString() ?? "";
                SetNeedsDraw();
            });
        }
    }

    public override void SetContent(object content)
    {
        if (content is T typedContent)
        {
            SetContent(typedContent);
            RaiseContentChanged();
        }
        else
        {
            throw new ArgumentException("Invalid content type");
        }
    }
}