using Timberborn.ModManagerScene;

namespace Kyler.TipsyTail
{
    // The game creates and starts every IModStarter in a mod's DLLs once, at launch, before any save is loaded, and
    // passes the folder it loaded the mod from. That folder is the only reliable way to find files next to the mod,
    // such as `water.cfg`. Nothing else happens here: the mod's components come from its blueprints and configurator.
    public sealed class TipsyTailModStarter : IModStarter
    {
        internal static string ModPath { get; private set; }

        public void StartMod(IModEnvironment modEnvironment)
        {
            ModPath = modEnvironment.ModPath;
        }
    }
}
