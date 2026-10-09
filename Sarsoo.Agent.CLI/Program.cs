using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Sarsoo.Agent.CLI.UI;

Logging.InitLogging();

RootCommand rootCommand = new("Sarsoo Agent")
{
    // Subcommands = { sub1Command }
};

rootCommand.SetAction(async (pr, ct) =>
{
    var host = InitTui.GetTuiHost(args);

    ct.Register(() =>
    {
        host.StopAsync(ct);
    });

    await host.RunAsync(ct);
});

return await rootCommand.Parse(args).InvokeAsync();