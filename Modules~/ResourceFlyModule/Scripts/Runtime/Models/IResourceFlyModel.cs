using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Models
{
    internal interface IResourceFlyModel
    {
        ResourceFlyOptionsCVO Options { get; }

        /// <summary>The resource under the key, made on the first ask.</summary>
        ResourceVO Get(string key);

        void SetSource(string source, RectTransform rect);
        void RemoveSource(string source);
        bool TryGetSource(string source, out RectTransform rect);

        /// <summary>The named look, from CD_ResourceFly's Looks.</summary>
        bool TryGetLook(string name, out ResourceFlyLookVO look);
    }
}