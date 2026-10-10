using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class UserMessage: BaseMessage
{
    public ChatMessage Content { get; protected set; }

    public UserMessage(ChatMessage message) : base()
    {
        Content = message;
        Title = "";
        SchemeName = "UserMessage";
        Text = Content.Text;
    }
}