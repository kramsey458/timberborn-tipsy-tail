using System.IO;

namespace Kyler.TipsyTail
{
    // Where the optional `water.cfg` lives. Free of game and Unity types so the Tests project can link it.
    internal static class TipsyTailPaths
    {
        internal const string ConfigFileName = "water.cfg";

        // The file sits in the mod folder the game loaded the mod from (IModEnvironment.ModPath, kept by
        // TipsyTailModStarter), whatever that folder is called and wherever it is. The game loads mod DLLs from bytes
        // (ModCodeStarter.LoadAssemblies), so Assembly.Location is always empty and cannot stand in for it. If the
        // starter never ran, fall back to the documented install folder, Documents/Timberborn/Mods/TipsyTail.
        internal static string ResolveConfigPath(string modPath, string documents)
        {
            if (!string.IsNullOrEmpty(modPath)) return Path.Combine(modPath, ConfigFileName);
            if (string.IsNullOrEmpty(documents)) return null;
            return Path.Combine(documents, "Timberborn", "Mods", "TipsyTail", ConfigFileName);
        }
    }
}
