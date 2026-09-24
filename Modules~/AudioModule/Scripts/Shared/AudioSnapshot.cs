namespace Modules.AudioModule.Shared
{
    /// <summary>
    /// A mixer snapshot, by the name it has in the mixer. The three below are in the mixer the
    /// module ships; a game that files a mixer of its own names its snapshots with
    /// <c>new AudioSnapshot("Boss")</c>.
    /// </summary>
    public readonly struct AudioSnapshot
    {
        /// <summary>Everything at the level the player set.</summary>
        public static readonly AudioSnapshot Default = new("Default");

        /// <summary>Music and ambience pulled down so a voice, a popup or a reward reads over them.</summary>
        public static readonly AudioSnapshot Ducked = new("Ducked");

        /// <summary>The game held: music muffled, effects down.</summary>
        public static readonly AudioSnapshot Paused = new("Paused");

        public readonly string Name;

        public AudioSnapshot(string name)
        {
            Name = name;
        }

        public override string ToString() => Name ?? "(no snapshot)";
    }
}
