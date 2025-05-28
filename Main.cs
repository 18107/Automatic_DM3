using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;

namespace Automatic_DM3
{
    public class Main
    {
        public static UnityModManager.ModEntry mod { get; private set; }

        internal static Settings settings { get; private set; }

        internal static readonly DM3List DM3s = new DM3List();

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            Harmony harmony = null;
            try
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", e);
                harmony?.UnpatchAll();
                return false;
            }

            //Setup GUI
            settings = Settings.Load<Settings>(modEntry);
            settings.PostLoad();
            mod.OnGUI += settings.Draw;
            mod.OnSaveGUI += settings.Save;
            mod.OnToggle += (mod, value) => { DM3s.ForEach(s => s.OnEndControl()); return true; };
            mod.OnFixedUpdate += DM3s.FixedUpdate;

            return true; //Loaded successfully
        }
    }
}
