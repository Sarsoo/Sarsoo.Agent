using System.CommandLine;
using Sarsoo.Agent.CLI.UI;
using Sarsoo.Agent.CLI.UI.TUI;
using Sarsoo.Agent.CLI.UI.Inline;

Logging.InitLogging();

Option<bool> themeInfoOption = new("--theme-info")
{
    Description = "Print the active Terminal.Gui theme and exit."
};

RootCommand rootCommand = new("Sarsoo Agent");
rootCommand.Options.Add(themeInfoOption);

rootCommand.SetAction(async (pr, ct) =>
{
    await InitTui.CommandAction(args, themeInfoOption, pr, ct);
});

// rootCommand.SetAction(async (pr, ct) =>
// {
//     await InitInline.CommandAction(args, pr, ct);
// });

return await rootCommand.Parse(args).InvokeAsync();
