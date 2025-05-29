using DV.ThingTypes;
using HarmonyLib;

namespace Automatic_DM1U
{
    [HarmonyPatch(typeof(TrainCar))]
    internal class TrainCarPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void Awake(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM1U)
            {
                Main.DM1Us.Add(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("AwakeForPooledCar")]
        static void AwakeForPooledCar(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM1U)
            {
                Main.DM1Us.Add(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("PrepareForDestroy")]
        static void PrepareForDestroy(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoDM1U)
            {
                Main.DM1Us.Remove(__instance);
            }
        }
    }
}
