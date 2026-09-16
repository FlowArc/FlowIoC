using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The framework's channels as they are shipped: the colour each is drawn in, whether it is on
    /// for somebody who has not touched it, and the tag on the front of its lines. These used to be
    /// rows in a committed settings asset, rebuilt from code on every load and then written back -
    /// so every project carried a copy of the same table and every module added or field renamed
    /// landed in the same diff. Now they are the table.
    ///
    /// The colours are the values the console was tuned to by eye, so a project that has just
    /// installed the package reads the same console the framework was developed against. The
    /// defaults are the same judgement: the game's traffic - Signal and Command - and what Unity
    /// wrote are on, and the machinery underneath them is off until a reader wants it.
    /// </summary>
    public class SystemLogChannelTable
    {
        /// <summary>Every framework channel in enum order. <see cref="SystemLogType.All"/> is not a channel.</summary>
        public IEnumerable<FlowLogChannel> All()
        {
            foreach (SystemLogType type in Enum.GetValues(typeof(SystemLogType)))
            {
                if (type == SystemLogType.All) continue;

                string name = type.ToString();
                Color color = ColorOf(type);

                yield return new FlowLogChannel(name, type, color, IsOnByDefault(type), ProfileOf(type, name, color));
            }
        }

        private static Color ColorOf(SystemLogType type)
        {
            switch (type)
            {
                case SystemLogType.Context: return new Color(0.11f, 1f, 0.535f);
                case SystemLogType.Injection: return new Color(0.104f, 0.809f, 0.528f);
                case SystemLogType.Signal: return new Color(1f, 0.78f, 0.224f);
                case SystemLogType.SignalOperation: return new Color(0.787f, 0.614f, 0.176f);
                case SystemLogType.Command: return Color.cyan;
                case SystemLogType.CommandOperation: return new Color(0f, 0.667f, 0.667f);
                case SystemLogType.Function: return new Color(0.231f, 0.765f, 1f);
                case SystemLogType.Screen: return new Color(0.953f, 0.912f, 0.211f);
                case SystemLogType.Pool: return new Color(0.629f, 0.533f, 1f);
                case SystemLogType.Asset: return new Color(0.629f, 0.533f, 1f);
                // A channel Unity writes has no colour of its own. Its rows carry an icon - the Unity
                // logo, the script icon, the shader icon - which says whose line it is better than a
                // stripe could, so the stripe and the swatch in the Filters panel draw nothing.
                case SystemLogType.Unity:
                case SystemLogType.Compiler:
                case SystemLogType.Shader:
                    return Color.clear;
                default: return Color.white;
            }
        }

        private static bool IsOnByDefault(SystemLogType type)
        {
            switch (type)
            {
                case SystemLogType.Signal:
                case SystemLogType.Command:
                case SystemLogType.Unity:
                case SystemLogType.Compiler:
                case SystemLogType.Shader:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// The tag follows the channel: same name, same colour, so the two never say different
        /// things about the same line. A channel Unity writes has an icon for a tag - the Unity
        /// logo, the script icon, the shader icon - which the console window draws on the row
        /// itself, so its lines carry nothing in the text: a line Unity wrote is recorded as Unity
        /// wrote it.
        /// </summary>
        private static FlowLogProfile ProfileOf(SystemLogType type, string name, Color color)
        {
            bool writtenByUnity = type == SystemLogType.Unity
                                  || type == SystemLogType.Compiler
                                  || type == SystemLogType.Shader;

            return writtenByUnity ? null : new FlowLogProfile().SetPrefix("[" + name + "]", FlowTextStyle.None, color);
        }
    }
}
