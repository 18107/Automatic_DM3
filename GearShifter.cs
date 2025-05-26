using DV.Simulation.Controllers;
using LocoSim.Implementations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Automatic_DM3
{
    internal class GearShifter
    {
        private readonly TrainCar car;
        private readonly Port engineRPMPort;
        private readonly Port gearA;
        private readonly Port gearB;
        private readonly ThrottleControl throttle;
        private readonly DynamicBrakeControl dynamicBrake;
        private readonly ReverserControl reverser;

        private bool changingGears = false;
        private Coroutine gearChange;

        internal GearShifter(TrainCar DM3)
        {
            car = DM3;
            DM3.SimController.simFlow.TryGetPort("de.RPM", out engineRPMPort);
            DM3.SimController.simFlow.TryGetPort("gearInputA.CONTROL_EXT_IN", out gearA);
            DM3.SimController.simFlow.TryGetPort("gearInputB.CONTROL_EXT_IN", out gearB);
            throttle = DM3.SimController.controlsOverrider.Throttle;
            dynamicBrake = DM3.SimController.controlsOverrider.DynamicBrake;
            reverser = DM3.SimController.controlsOverrider.Reverser;
        }

        internal void Update()
        {
            if (!Main.mod.Enabled) return;

            if (changingGears) return;
            if (reverser.Value == 0.5f) return;

            if (engineRPMPort.Value > Main.settings.maxRPM && !(gearA.Value == 1 && gearB.Value == 1))
            {
                changingGears = true;
                gearChange = car.StartCoroutine(ChangeGear(1));
                return;
            }

            if (engineRPMPort.Value < Main.settings.minRPM && !(gearA.Value == 0 && gearB.Value == 0))
            {
                changingGears = true;
                gearChange = car.StartCoroutine(ChangeGear(-1));
                return;
            }
        }

        internal void Stop()
        {
            car.StopCoroutine(gearChange);
        }

        private static readonly (float, float)[] gearOrder = { (0, 0), (0, 0.5f), (0.5f, 0), (0.5f, 0.5f), (1, 0), (1, 0.5f), (0.5f, 1), (1, 1) };
        private static readonly Dictionary<(float, float), int> gearLookup = new Dictionary<(float, float), int>() {
            { (0, 0), 0 },
            { (0, 0.5f), 1 },
            { (0.5f, 0), 2 },
            { (0.5f, 0.5f), 3 },
            { (0, 1), 4 },
            { (1, 0), 4 },
            { (1, 0.5f), 5 },
            { (0.5f, 1), 6 },
            { (1, 1), 7 }
        };

        private static (float, float) NextGear(float gearA, float gearB, int amount)
        {
            int gear = gearLookup[(gearA, gearB)]; //Relies on the floating point values being stored exactly
            int newGear = Mathf.Clamp(gear + amount, 0, gearOrder.Length-1);
            return gearOrder[newGear];
        }

        private IEnumerator ChangeGear(int amount)
        {
            float t = throttle.Value;
            float b = dynamicBrake.Value;

            if (t != 0) throttle.Set(0);
            if (b != 0) dynamicBrake.Set(0);
            if (t != 0 || b != 0) yield return new WaitForSeconds(Main.settings.shiftDelay);

            (float nextA, float nextB) = NextGear(gearA.Value, gearB.Value, amount);
            gearA.ExternalValueUpdate(nextA);
            gearB.ExternalValueUpdate(nextB);
            if (t != 0 || b != 0) yield return new WaitForSeconds(Main.settings.shiftDelay);

            if (t != 0) throttle.Set(t);
            if (b != 0) dynamicBrake.Set(b);
            yield return new WaitForSeconds(Main.settings.postShiftDelay);

            changingGears = false;
        }
    }
}
