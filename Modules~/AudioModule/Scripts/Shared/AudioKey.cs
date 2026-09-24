using System;

namespace Modules.AudioModule.Shared
{
    /// <summary>
    /// One sound, named by the module that plays it. The id is <c>&lt;Module&gt;/&lt;Name&gt;</c> -
    /// <c>GameplayModule/Jump</c> - so the key also says which bank holds it.
    ///
    /// Audio declares no key of its own. A module that plays sound adds its keys in a part of this
    /// struct that lives in its own folder, <c>Scripts/AudioKeys/AudioKey.&lt;Module&gt;.cs</c>, and
    /// the <c>Modules.Audio.Shared.asmref</c> beside it compiles that part into this assembly:
    /// <code>
    /// public readonly partial struct AudioKey
    /// {
    ///     public static class Gameplay
    ///     {
    ///         public static readonly AudioKey Jump = new("GameplayModule/Jump");
    ///     }
    /// }
    /// </code>
    /// A step then plays it by name: <c>.ToSequence&lt;IAudioService.Commands.Play&gt;(AudioKey.Gameplay.Jump)</c>.
    /// </summary>
    public readonly partial struct AudioKey : IEquatable<AudioKey>
    {
        public const char SEPARATOR = '/';

        public readonly string Id;

        public AudioKey(string id)
        {
            Id = id;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Id);

        /// <summary>The module whose bank holds this sound - the part of the id before the separator.</summary>
        public string Bank
        {
            get
            {
                int index = Id?.IndexOf(SEPARATOR) ?? -1;
                return index > 0 ? Id.Substring(0, index) : string.Empty;
            }
        }

        /// <summary>The sound's own name - the part of the id after the separator.</summary>
        public string Name
        {
            get
            {
                int index = Id?.IndexOf(SEPARATOR) ?? -1;
                return index >= 0 ? Id.Substring(index + 1) : Id ?? string.Empty;
            }
        }

        public bool Equals(AudioKey other) => string.Equals(Id, other.Id, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is AudioKey other && Equals(other);

        public override int GetHashCode() => Id != null ? StringComparer.Ordinal.GetHashCode(Id) : 0;

        public override string ToString() => IsEmpty ? "(no key)" : Id;

        public static bool operator ==(AudioKey left, AudioKey right) => left.Equals(right);

        public static bool operator !=(AudioKey left, AudioKey right) => !left.Equals(right);
    }
}
