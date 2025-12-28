using DV.ThingTypes;
using HarmonyLib;

namespace Automatic_DM3
{
    [HarmonyPatch(typeof(TrainCar))]
    internal class TrainCarPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void Awake(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM3)
            {
                Main.DM3s.Add(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("AwakeForPooledCar")]
        static void AwakeForPooledCar(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM3)
            {
                Main.DM3s.Add(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("PrepareForDestroy")]
        static void PrepareForDestroy(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM3)
            {
                Main.DM3s.Remove(__instance);
            }
        }
    }
}
