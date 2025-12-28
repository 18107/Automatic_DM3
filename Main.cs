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
            modEntry.OnGUI += settings.OnGui;
            modEntry.OnSaveGUI += settings.Save;
            modEntry.OnFixedUpdate += DM3s.FixedUpdate;
            modEntry.OnToggle += (mod, value) =>
            {
                if (value)
                {
                    //Find any DM3s not already registered in case the mod was disabled when the game was loaded
                    DM3s.AddAll();
                }
                else
                {
                    DM3s.ForEach(s => s.OnEndControl());
                }
                //Mod toggled sucessfully
                return true;
            };

            return true; //Loaded successfully
        }
    }
}
