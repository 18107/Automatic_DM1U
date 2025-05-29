using HarmonyLib;
using LocoSim.Implementations;

namespace Automatic_DM1U
{
    [HarmonyPatch(typeof(SmoothTransmission), "Tick")]
    internal class SmoothTransmissionPatch
    {
        static void Postfix(SmoothTransmission __instance)
        {
            //If CVT selected
            if (!Main.mod.Enabled) return;
            if (!Main.settings.CVTActive) return;

            GearShifter shifter = Main.DM1Us.TransmissionLookup(__instance);
            if (shifter == null) return;

            //Set the torque and gear ratio
            __instance.torqueOut.Value = __instance.torqueIn.Value * shifter.CVTRatio * __instance.transmissionEfficiency;
            __instance.gearRatioReadOut.Value = shifter.CVTRatio;
        }
    }
}
