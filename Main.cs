using DV.ThingTypes;
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

        private static GearShifter gearShifter;

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
            mod.OnGUI += settings.Draw;
            mod.OnSaveGUI += settings.Save;
            mod.OnToggle += (mod, value) => { gearShifter?.Stop(); return true; };

            PlayerManager.CarChanged += (car) => gearShifter = (car?.carType == TrainCarType.LocoDM3 ? new GearShifter(car) : null);
            mod.OnFixedUpdate += (mod, dt) => gearShifter?.Update();

            return true; //Loaded successfully
        }
    }
}
