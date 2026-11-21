namespace Sarsoo.Agent.Tool.Policy;

public static class VerbPolicy
{
    public static bool VerbIsAnyOf(string[] argv, IEnumerable<string> toFind)
    {
        var verb = argv.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (verb is null)
        {
            return false;
        }

        return toFind.Any(x => verb.Equals(x, StringComparison.OrdinalIgnoreCase));
    }
}