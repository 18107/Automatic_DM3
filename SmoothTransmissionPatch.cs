using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;

namespace Automatic_DM3
{
    [HarmonyPatch(typeof(SmoothTransmission), "Tick")]
    internal class SmoothTransmissionPatch
    {
        static void Postfix(SmoothTransmission __instance)
        {
            //If CVT selected
            if (!Main.mod.Enabled) return;
            if (!Main.settings.CVTActive) return;

            GearShifter shifter = Main.DM3s.TransmissionLookup(__instance);
            if (shifter == null) return;

            //Set the torque and gear ratio
            float ratio = Mathf.Sqrt(shifter.CVTRatio); //The two gearboxes multiply their ratios to get the final ratio
            __instance.torqueOut.Value = __instance.torqueIn.Value * ratio * __instance.transmissionEfficiency;
            __instance.gearRatioReadOut.Value = ratio;
        }
    }
}
