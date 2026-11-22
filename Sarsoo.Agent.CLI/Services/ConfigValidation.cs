using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Options;
using Spectre.Console;

namespace Sarsoo.Agent.CLI.Services;

public class ConfigValidation(
    IHostApplicationLifetime applicationLifetime,
    IOptions<ProviderOptions> providerOptions
): IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        bool failed = false;
        if (string.IsNullOrWhiteSpace(providerOptions.Value.Url))
        {
            AnsiConsole.MarkupLineInterpolated($"[bold red]✗ Error:[/] Config ~/.config/reeve/config.json is missing a provider url... (provider.url)");
            failed = true;
        }
        else if (string.IsNullOrWhiteSpace(providerOptions.Value.Key))
        {
            AnsiConsole.MarkupLineInterpolated($"[bold red]✗ Error:[/] Config ~/.config/reeve/config.json is missing a provider key... (provider.key or export PROVIDER__KEY='')");
            failed = true;
        }
        else if (string.IsNullOrWhiteSpace(providerOptions.Value.Model))
        {
            AnsiConsole.MarkupLineInterpolated($"[bold red]✗ Error:[/] Config ~/.config/reeve/config.json is missing a model ID... (provider.model)");
            failed = true;
        }

        if (failed)
        {
            applicationLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}