using DV.Simulation.Cars;
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
        private readonly Port driveshaftRPMPort;
        private readonly Port wheelSpeedPort;
        private readonly Port gearA;
        private readonly Port gearB;
        private readonly ThrottleControl throttle;
        private readonly DynamicBrakeControl dynamicBrake;
        private readonly ReverserControl reverser;

        private bool changingGears = false;
        private bool endGearChange = false;
        private float gearUpTime = 0;
        private float gearDownTime = 0;

        internal readonly SmoothTransmission transmissionA;
        internal readonly SmoothTransmission transmissionB;
        internal float CVTRatio { get; private set; }

        private static readonly float[] ratios = { 20, 15, 12, 9, 8, 6, 4.5f, 3 };
        private static readonly (float, float)[] gearOrder = { (0, 0), (0, 0.5f), (0.5f, 0), (0.5f, 0.5f), (1, 0), (1, 0.5f), (0.5f, 1), (1, 1) };
        private static readonly Dictionary<(float, float), int> gearLookup = new Dictionary<(float, float), int>() {
            { (0, 0), 0 },
            { (0, 0.5f), 1 },
            { (0.5f, 0), 2 },
            { (0.5f, 0.5f), 3 },
            { (1, 0), 4 },
            { (0, 1), 4 },
            { (1, 0.5f), 5 },
            { (0.5f, 1), 6 },
            { (1, 1), 7 }
        };

        internal GearShifter(TrainCar DM3)
        {
            SimulationFlow simFlow = DM3.SimController.simFlow;
            BaseControlsOverrider controlsOverrider = DM3.SimController.controlsOverrider;

            car = DM3;
            simFlow.TryGetPort("de.RPM", out engineRPMPort);
            simFlow.TryGetPort("driveShaftRpmCalculator.DRIVE_SHAFT_RPM", out driveshaftRPMPort);
            simFlow.TryGetPort("traction.WHEEL_SPEED_KMH_EXT_IN", out wheelSpeedPort);
            simFlow.TryGetPort("gearInputA.CONTROL_EXT_IN", out gearA);
            simFlow.TryGetPort("gearInputB.CONTROL_EXT_IN", out gearB);
            throttle = controlsOverrider.Throttle;
            dynamicBrake = controlsOverrider.DynamicBrake;
            reverser = controlsOverrider.Reverser;

            transmissionA = simFlow.OrderedSimComps[31] as SmoothTransmission;
            transmissionB = simFlow.OrderedSimComps[32] as SmoothTransmission;
            CVTRatio = ratios[gearLookup[(gearA.Value, gearB.Value)]];
        }

        internal void Update()
        {
            if (!Main.mod.Enabled) return;

            if (changingGears) return;
            if (reverser.Value == 0.5f) return;

            //See also SmoothTransmissionPatch
            if (Main.settings.CVT)
            {
                //Engine RPM slips slightly above and below the driveshaft RPM.
                float RPMoffset = (throttle.Value - dynamicBrake.Value) * 80;

                //Calculate gear ratio for ideal engine RPM
                float newRatio = Main.settings.targetRPM / (Math.Abs(driveshaftRPMPort.Value) + RPMoffset) * CVTRatio;

                //Limit gear ratio
                if (Main.settings.IVT)
                {
                    newRatio = Mathf.Clamp(newRatio, float.Epsilon, 80); //Technical/traction limitations
                }
                else
                {
                    newRatio = Mathf.Clamp(newRatio, 3, 20); //Default gear range
                }
                //Limit gear change speed
                CVTRatio = Mathf.Clamp(newRatio / CVTRatio, 0.98f, 1.02f) * CVTRatio;
                return;
            }

            //Upshift if RPM is greater than max
            if ((!(gearA.Value == 1 && gearB.Value == 1)) && engineRPMPort.Value > Main.settings.maxRPM)
            {
                changingGears = true;
                endGearChange = false;
                if (Time.time < gearUpTime + Main.settings.doubleShiftTime)
                {
                    car.StartCoroutine(ChangeGear(2));
                }
                else
                {
                    car.StartCoroutine(ChangeGear(1));
                }
                return;
            }

            //Downshift if RPM would be less than max after downshifting. RPMoffset to prevent hunting
            int currentGear = gearLookup[(gearA.Value, gearB.Value)];
            if (currentGear > 0 && engineRPMPort.Value * ratios[currentGear - 1] / ratios[currentGear] < Main.settings.maxRPM - Main.settings.RPMoffset)
            {
                changingGears = true;
                endGearChange = false;
                if (currentGear > 1 &&
                    engineRPMPort.Value * ratios[currentGear - 2] / ratios[currentGear] < Main.settings.maxRPM - Main.settings.RPMoffset &&
                    Time.time < gearDownTime + Main.settings.doubleShiftTime)
                {
                    car.StartCoroutine(ChangeGear(-2));
                }
                else
                {
                    car.StartCoroutine(ChangeGear(-1));
                }
                return;
            }
        }

        internal void StopShift()
        {
            endGearChange = true;
        }

        internal void OnEndControl()
        {
            if (!Main.mod.Enabled) return;

            StopShift();

            //Select the most suitable gear ratio to leave the locomotive in
            if (Main.settings.CVT && reverser.Value != 0.5f)
            {
                SwitchGearboxType(false);
            }
        }

        internal void SwitchGearboxType(bool CVT)
        {
            if (CVT)
            {
                if (Main.mod.Enabled) StopShift();

                //Select current gear ratio for CVT starting point
                CVTRatio = ratios[gearLookup[(gearA.Value, gearB.Value)]];
            }
            else
            {
                if (!Main.mod.Enabled) return;
                if (reverser.Value == 0.5) return;

                //Find adequate gear for current speed
                float[] speeds = { 7, 9, 12, 15, 17, 23, 30, float.PositiveInfinity };
                int neededGear = Array.FindIndex(speeds, x => Math.Abs(wheelSpeedPort.Value) < x);
                int currentGear = gearLookup[(gearA.Value, gearB.Value)];

                //Change gear
                if (neededGear != currentGear)
                {
                    changingGears = true;
                    endGearChange = true; //skip waiting times
                    car.StartCoroutine(ChangeGear(neededGear - currentGear));
                }
            }
        }

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

            //Set throttle and brake to 0
            if (t != 0) throttle.Set(0);
            if (b != 0) dynamicBrake.Set(0);
            //Prevent (undo) throttle and dynamic brake changes while shifting
            throttle.ControlUpdated += throttleUpdated;
            dynamicBrake.ControlUpdated += brakeUpdated;

            //If throttle or brake needed to be moved
            if (t != 0 || b != 0)
            {
                //Wait for shiftDelay
                float time = Time.time;
                while (Time.time < time + Main.settings.shiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            //Change gear
            (float nextA, float nextB) = NextGear(gearA.Value, gearB.Value, amount);
            gearA.ExternalValueUpdate(nextA);
            gearB.ExternalValueUpdate(nextB);

            //If throttle or brake needed to be moved
            if (t != 0 || b != 0)
            {
                //Wait for shiftDelay
                float time = Time.time;
                while (Time.time < time + Main.settings.shiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            //Stop watching throttle and brake
            throttle.ControlUpdated -= throttleUpdated;
            dynamicBrake.ControlUpdated -= brakeUpdated;
            yield return null; //To prevent a race condition

            //Reset throttle and dynamic brake to previous values + any changes while shifting
            if (t != 0) throttle.Set(t);
            if (b != 0) dynamicBrake.Set(b);

            //Wait for postShiftDelay
            {
                float time = Time.time;
                while (Time.time < time + Main.settings.postShiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            changingGears = false;
            if (amount > 0)
            {
                gearUpTime = Time.time;
            }
            else
            {
                gearDownTime = Time.time;
            }

            void throttleUpdated(float value)
            {
                if (value == 0) return;

                //save value to increase throttle after shifting
                t += value;

                //Wait one frame to reset throttle
                car.StartCoroutine(SetThrottle());
                IEnumerator SetThrottle()
                {
                    yield return null;
                    throttle.Set(0);
                }
            }

            void brakeUpdated(float value)
            {
                if (value == 0) return;

                //record value to increase dynamic brake after shifting
                b += value;

                //Wait one frame to reset brake
                car.StartCoroutine(SetBrake());
                IEnumerator SetBrake()
                {
                    yield return null;
                    dynamicBrake.Set(0);
                }
            }
        }
    }
}
