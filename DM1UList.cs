using DV.Utils;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace Automatic_DM1U
{
    internal class DM1UList
    {
        private readonly Dictionary<TrainCar, GearShifter> DM1Us = new Dictionary<TrainCar, GearShifter>();
        private readonly Dictionary<SmoothTransmission, GearShifter> transmissionLookup = new Dictionary<SmoothTransmission, GearShifter>();

        internal void AddAll()
        {
            SingletonBehaviour<CarSpawner>.Instance?.AllLocos?.ForEach(l => { if (l.carType == DV.ThingTypes.TrainCarType.LocoDM1U && !DM1Us.ContainsKey(l)) Add(l); });
        }

        internal void Add(TrainCar car)
        {
            if (DM1Us.ContainsKey(car))
            {
                Remove(car);
            }

            GearShifter shifter = new GearShifter(car);
            DM1Us.Add(car, shifter);

            SmoothTransmission transmission = car.SimController.SimulationFlow.OrderedSimComps[27] as SmoothTransmission;
            transmissionLookup.Add(transmission, shifter);

            if (Main.settings.CVT)
            {
                transmission.powerShiftRpmThreshold = 3000;
            }
        }

        internal void Remove(TrainCar car)
        {
            if (!DM1Us.ContainsKey(car)) return;

            DM1Us[car].StopShift();
            DM1Us.Remove(car);
            SmoothTransmission transmission = car.SimController.SimulationFlow.OrderedSimComps[27] as SmoothTransmission;
            transmissionLookup.Remove(transmission);

            transmission.powerShiftRpmThreshold = 500;
        }

        internal void SwitchGearboxType(bool CVT)
        {
            ForEach(s => s.SwitchGearboxType(CVT));

            //Prevent damage from moving gear levers while in CVT mode
            float powerShiftRPMThreshold = CVT ? 3000 : 500;
            transmissionLookup.Keys.ToList().ForEach(t => t.powerShiftRpmThreshold = powerShiftRPMThreshold);
        }

        internal void ForEach(Action<GearShifter> action)
        {
            DM1Us.Values.ToList().ForEach(action);
        }

        internal GearShifter TransmissionLookup(SmoothTransmission transmission)
        {
            transmissionLookup.TryGetValue(transmission, out GearShifter shifter);
            return shifter;
        }

        internal void FixedUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (!Main.mod.Enabled) return;

            ForEach(s => s.Update());
        }
    }
}
