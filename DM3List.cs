using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace Automatic_DM3
{
    internal class DM3List
    {
        private readonly Dictionary<TrainCar, GearShifter> DM3s = new Dictionary<TrainCar, GearShifter>();
        private readonly Dictionary<SmoothTransmission, GearShifter> transmissionLookup = new Dictionary<SmoothTransmission, GearShifter>();

        internal void Add(TrainCar car)
        {
            if (DM3s.ContainsKey(car))
            {
                Remove(car);
            }

            GearShifter shifter = new GearShifter(car);
            DM3s.Add(car, shifter);

            SmoothTransmission smoothTransmissionA = car.SimController.simFlow.OrderedSimComps[31] as SmoothTransmission;
            SmoothTransmission smoothTransmissionB = car.SimController.simFlow.OrderedSimComps[32] as SmoothTransmission;
            transmissionLookup.Add(smoothTransmissionA, shifter);
            transmissionLookup.Add(smoothTransmissionB, shifter);

            //Prevent gear lever damage while in CVT mode
            if (Main.settings.CVT)
            {
                smoothTransmissionA.powerShiftRpmThreshold = 1500;
                smoothTransmissionB.powerShiftRpmThreshold = 1500;
            }
        }

        internal void Remove(TrainCar car)
        {
            if (!DM3s.ContainsKey(car)) return;

            DM3s[car].StopShift();
            DM3s.Remove(car);
            SmoothTransmission smoothTransmissionA = car.SimController.simFlow.OrderedSimComps[31] as SmoothTransmission;
            SmoothTransmission smoothTransmissionB = car.SimController.simFlow.OrderedSimComps[32] as SmoothTransmission;
            transmissionLookup.Remove(smoothTransmissionA);
            transmissionLookup.Remove(smoothTransmissionB);

            smoothTransmissionA.powerShiftRpmThreshold = 400; //SmoothTransmissionDefinition.powerShiftRpmThreshold
            smoothTransmissionB.powerShiftRpmThreshold = 400;
        }

        internal void SwitchGearboxType(bool CVT)
        {
            ForEach(s => s.SwitchGearboxType(CVT));

            //Prevent damage from moving gear levers while in CVT mode
            float powerShiftRpmThreshold = CVT ? 1500 : 400;
            transmissionLookup.Keys.ToList().ForEach(t => t.powerShiftRpmThreshold = powerShiftRpmThreshold);
        }

        internal void ForEach(Action<GearShifter> action)
        {
            DM3s.Values.ToList().ForEach(action);
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
