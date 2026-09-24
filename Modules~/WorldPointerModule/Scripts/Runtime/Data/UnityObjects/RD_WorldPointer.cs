using System.Collections.Generic;
using Modules.WorldPointerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.UnityObjects
{
    /// <summary>
    /// What the service holds right now, for the Inspector during play: one row per channel, with
    /// the display that draws it and every target it points at. It answers "why is nothing shown" -
    /// a row with no display is waiting for its screen. The rows are the Model's own objects,
    /// written in place as they change; nothing reads the serialized fields back.
    /// </summary>
    [CreateAssetMenu(fileName = "RD_WorldPointer", menuName = "FlowIoC/WorldPointerModule/Data/RD_WorldPointer")]
    public class RD_WorldPointer : ScriptableObject
    {
        [SerializeField] private List<WorldPointerChannelRVO> _channels = new();

        public List<WorldPointerChannelRVO> Channels => _channels;
    }
}