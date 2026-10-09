using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Sarsoo.Agent.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        // await using var agent = new Session.SAgent(client, null, null);

        // var result = await agent.RunAsync("Run ls, what's the output?", cancellationToken: TestContext.Current.CancellationToken);
        
        // var answer = result;
        
        // await foreach (var update in agent.RunStreamingAsync("What is AI?", cancellationToken: TestContext.Current.CancellationToken))
        // {
        //     TestContext.Current.TestOutputHelper?.Write(update.ToString());
        // }
    }
}