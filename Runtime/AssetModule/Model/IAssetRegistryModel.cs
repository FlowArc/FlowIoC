using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;

namespace FlowIoC.AssetModule.Model
{
    internal interface IAssetRegistryModel
    {
        /// <summary>Every loaded or loading asset by registry key. Read here; change through the methods below.</summary>
        IReadOnlyDictionary<string, AssetEntryVO> Entries { get; }

        /// <summary>Every group by id. Read here; change through the methods below.</summary>
        IReadOnlyDictionary<string, AssetGroupVO> Groups { get; }

        AssetEntryVO GetOrCreateEntry(string registryKey);
        AssetGroupVO GetOrCreateGroup(string groupId);

        /// <summary>One claim on the entry: a group's ownership, recorded on both sides, or an unscoped claim when groupId is null.</summary>
        void Claim(AssetEntryVO entry, string regKey, string groupId);

        /// <summary>Takes back what <see cref="Claim"/> recorded; an unscoped claim never goes below zero.</summary>
        void Unclaim(AssetEntryVO entry, string regKey, string groupId);

        /// <summary>Forgets the entry, takes its key out of every group that owned it, and releases its handle.</summary>
        void RemoveEntry(string regKey);

        void RemoveGroup(string groupId);

        void MarkGroupLoaded(string groupId);

        /// <summary>The load a later caller of the same key waits on, or null once it has landed.</summary>
        void SetLoading(AssetEntryVO entry, Task<object> loading);

        void Clear();
    }
}