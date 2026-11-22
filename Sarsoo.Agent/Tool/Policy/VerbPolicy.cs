namespace Sarsoo.Agent.Tool.Policy;

public static class VerbPolicy
{
    public static bool VerbIsAnyOf(List<string> argv, IEnumerable<string> toFind)
    {
        var verb = argv.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (verb is null)
        {
            return false;
        }

        return toFind.Any(x => verb.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool VerbSetIsAnyOf(List<string> argv, IEnumerable<IEnumerable<string>> toFind)
    {
        outer: foreach (var verbSet in toFind)
        {
            var v = verbSet.ToArray();

            if (argv.Count < v.Length)
            {
                continue outer;
            }

            for (var i = 0; i < v.Length; i++)
            {
                if (!argv[i].Equals(v[i], StringComparison.OrdinalIgnoreCase))
                {
                    continue outer;
                }
            }

            return true;
        }

        return false;
    }
}