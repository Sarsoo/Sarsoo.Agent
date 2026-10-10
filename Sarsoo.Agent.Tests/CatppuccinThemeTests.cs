using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.CLI.UI;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     Terminal.Gui configuration is process-global static state, so these tests opt out of
///     parallelization and restore the default configuration afterwards.
/// </summary>
[CollectionDefinition ("TerminalGuiGlobalState", DisableParallelization = true)]
public class TerminalGuiGlobalStateCollection;

[Collection ("TerminalGuiGlobalState")]
public class CatppuccinThemeTests
{
    private static string ReadEmbeddedConfig ()
    {
        var assembly = typeof (TuiService).Assembly;

        string? resourceName = assembly
                               .GetManifestResourceNames ()
                               .FirstOrDefault (n => n.EndsWith (TuiConfigurationExtensions.CONFIG_FILENAME, StringComparison.Ordinal));

        Assert.NotNull (resourceName);

        using Stream stream = assembly.GetManifestResourceStream (resourceName!)!;
        using StreamReader reader = new (stream);

        return reader.ReadToEnd ();
    }

    [Fact]
    public void EmbeddedConfig_IsNamedForTheLoaderAndDefinesBothThemes ()
    {
        var assembly = typeof (TuiService).Assembly;
        string? resourceName = assembly
                               .GetManifestResourceNames ()
                               .FirstOrDefault (n => n.EndsWith (TuiConfigurationExtensions.CONFIG_FILENAME, StringComparison.Ordinal));

        // The loader matches any entry-assembly resource whose name ends with "config.json".
        Assert.NotNull (resourceName);

        JsonObject root = JsonNode.Parse (ReadEmbeddedConfig ())!.AsObject ();
        Assert.Equal ("Catppuccin Mocha", (string?)root ["Theme"]);

        JsonObject themes = root ["Themes"]!.AsObject ();
        Assert.True (themes.ContainsKey ("Catppuccin Mocha"));
        Assert.True (themes.ContainsKey ("Catppuccin Latte"));
    }

    [Theory]
    [InlineData ("Catppuccin Mocha", "#cdd6f4", "#1e1e2e", "#cba6f7", "#11111b")]
    [InlineData ("Catppuccin Latte", "#4c4f69", "#eff1f5", "#8839ef", "#dce0e8")]
    public void Theme_AppliesExpectedBaseScheme (string themeName, string text, string baseBackground, string keyword, string codeBackground)
    {
        JsonObject config = JsonNode.Parse (ReadEmbeddedConfig ())!.AsObject ();
        config ["Theme"] = themeName;

        var builder = new TuiConfigurationBuilder ("sarsoo-agent-tests") { RuntimeConfig = config.ToJsonString () };

        try
        {
            builder.ApplyToStaticFacades ();

            Scheme scheme = SchemeManager.GetScheme (Schemes.Base);

            Assert.Equal (themeName, ThemeManager.GetCurrentThemeName ());
            Assert.Equal (Color.Parse (text), scheme.Normal.Foreground);
            Assert.Equal (Color.Parse (baseBackground), scheme.Normal.Background);
            Assert.Equal (Color.Parse (keyword), scheme.CodeKeyword.Foreground);
            Assert.Equal (Color.Parse (codeBackground), scheme.Code.Background);
        }
        finally
        {
            TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
        }
    }

    [Theory]
    [InlineData ("Catppuccin Mocha", "#1e1e2e", "#45475a", "#181825")]
    [InlineData ("Catppuccin Latte", "#eff1f5", "#bcc0cc", "#e6e9ef")]
    public void Theme_DefinesDistinctMessageSchemes (string themeName, string agentBackground, string userBackground, string reasoningBackground)
    {
        JsonObject config = JsonNode.Parse (ReadEmbeddedConfig ())!.AsObject ();
        config ["Theme"] = themeName;

        var builder = new TuiConfigurationBuilder ("sarsoo-agent-tests") { RuntimeConfig = config.ToJsonString () };

        try
        {
            builder.ApplyToStaticFacades ();

            Assert.Equal (Color.Parse (agentBackground), SchemeManager.GetScheme ("AgentMessage").Normal.Background);
            Assert.Equal (Color.Parse (userBackground), SchemeManager.GetScheme ("UserMessage").Normal.Background);
            Assert.Equal (Color.Parse (reasoningBackground), SchemeManager.GetScheme ("ReasoningMessage").Normal.Background);
            Assert.True (SchemeManager.GetScheme ("ReasoningMessage").Normal.Style.HasFlag (TextStyle.Italic));
        }
        finally
        {
            TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
        }
    }

    [Fact]
    public void MessageViews_SelectTheirThemeSchemes ()
    {
        Assert.Equal ("AgentMessage", new TextMessage (new TextContent ("hi")).SchemeName);
        Assert.Equal ("ReasoningMessage", new ReasoningMessage (new TextReasoningContent ("thinking")).SchemeName);
        Assert.Equal ("UserMessage", new UserMessage(new ChatMessage()).SchemeName);
        Assert.Equal ("FunctionCallMessage", new FunctionCallMessage (new FunctionCallContent ("id", "name")).SchemeName);
        Assert.Equal ("FunctionResultMessage", new FunctionResultMessage (new FunctionResultContent ("id", "result")).SchemeName);
    }
}
