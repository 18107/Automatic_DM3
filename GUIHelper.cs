using DV.Util.EventWrapper;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace Automatic_DM3
{
    internal class GUIHelper
    {
        private static readonly Type TexturesType = AccessTools.Inner(typeof(UnityModManager), "Textures");
        private static readonly FieldInfo QuestionField = AccessTools.Field(TexturesType, "Question");
        private static readonly FieldInfo mTooltipField = AccessTools.Field(typeof(UnityModManager.UI), "mTooltip");
        private static readonly FieldInfo questionStyleField = AccessTools.Field(typeof(UnityModManager.UI), "question");

        /// <summary>
        /// Draws a question mark that displays a tooltip when hovered
        /// </summary>
        /// <param name="tooltip"></param>
        /// <returns>true if the tooltip is currently being displayed</returns>
        public static bool DrawTooltip(string tooltip)
        {
            GUILayout.Box((Texture2D)QuestionField.GetValue(null), (GUIStyle)questionStyleField.GetValue(null));
            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                mTooltipField.SetValue(UnityModManager.UI.Instance, new GUIContent(tooltip));
                return true;
            }
            return false;
        }
    }
}
