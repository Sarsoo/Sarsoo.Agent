using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Sarsoo.Agent.CLI.UI;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

Logging.InitLogging();

Option<bool> themeInfoOption = new("--theme-info")
{
    Description = "Print the active Terminal.Gui theme and exit."
};

RootCommand rootCommand = new("Sarsoo Agent");
rootCommand.Options.Add(themeInfoOption);

rootCommand.SetAction(async (pr, ct) =>
{
    if (pr.GetValue(themeInfoOption))
    {
        PrintThemeInfo();

        return;
    }

    var host = InitTui.GetTuiHost(args);

    ct.Register(() =>
    {
        host.StopAsync(ct);
    });

    await host.RunAsync(ct);
});

return await rootCommand.Parse(args).InvokeAsync();

static void PrintThemeInfo()
{
    // Touching a Terminal.Gui type loads the assembly, which runs its module initializer and
    // applies the theme config (including this app's embedded config.json).
    Scheme baseScheme = SchemeManager.GetScheme(Schemes.Base);

    Console.WriteLine($"Theme:     {ThemeManager.GetCurrentThemeName()}");
    Console.WriteLine($"Available: {string.Join(", ", ThemeManager.GetThemeNames())}");
    Console.WriteLine($"Base:      fg={baseScheme.Normal.Foreground} bg={baseScheme.Normal.Background}");
}
