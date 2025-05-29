using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using LocoSim.Implementations;
using System;
using System.Collections;
using UnityEngine;

namespace Automatic_DM1U
{
    internal class GearShifter
    {
        private readonly TrainCar car;
        private readonly Port engineRPMPort;
        private readonly Port driveshaftRPMPort;
        private readonly Port wheelSpeedPort;
        private readonly Port gearPort;
        private readonly Port currentGear;
        private readonly ThrottleControl throttle;
        private readonly ReverserControl reverser;

        private bool changingGears = false;
        private bool endGearChange = false;

        internal float CVTRatio { get; private set; }

        private static readonly float[] ratios = { 15, 15, 10.6f, 7.8f, 5.8f, 4.4f, 3.4f };

        public GearShifter(TrainCar DM1U)
        {
            SimulationFlow simFlow = DM1U.SimController.SimulationFlow;
            BaseControlsOverrider controlsOverrider = DM1U.SimController.controlsOverrider;

            car = DM1U;
            simFlow.TryGetPort("de.RPM", out engineRPMPort);
            simFlow.TryGetPort("driveShaftRpmCalculator.DRIVE_SHAFT_RPM", out driveshaftRPMPort);
            simFlow.TryGetPort("traction.WHEEL_SPEED_KMH_EXT_IN", out wheelSpeedPort);
            simFlow.TryGetPort("gearSelect.CONTROL_EXT_IN", out gearPort);
            simFlow.TryGetPort("gearSelect.GEAR", out currentGear);
            throttle = controlsOverrider.Throttle;
            reverser = controlsOverrider.Reverser;
            CVTRatio = ratios[(int)currentGear.Value];
        }

        internal void Update()
        {
            if (changingGears) return;
            if (reverser.Value == 0.5f || currentGear.Value == 0) return;

            //See also SmoothTransmissionPatch
            if (Main.settings.CVTActive)
            {
                //Engine RPM slips slightly above and below the driveshaft RPM.
                float RPMOffset = throttle.Value * 480 - 220;

                //Calculate gear ratio for ideal engine RPM
                float newRatio = Main.settings.targetRPM / (Mathf.Abs(driveshaftRPMPort.Value) + RPMOffset) * CVTRatio;

                //Limit gear ratio
                if (Main.settings.IVT)
                {
                    newRatio = Mathf.Clamp(newRatio, float.Epsilon, 80); //TODO
                }
                else
                {
                    newRatio = Mathf.Clamp(newRatio, 3.4f, 15); //Default gear range
                }

                //Limit gear change speed
                CVTRatio = Mathf.Clamp(newRatio / CVTRatio, 0.98f, 1.02f) * CVTRatio;
                return;
            }

            //Upshift if RPM is greater than max
            if (currentGear.Value < 6 && engineRPMPort.Value > Main.settings.maxRPM)
            {
                changingGears = true;
                endGearChange = false;
                car.StartCoroutine(ChangeGear(1));
                return;
            }

            //Downshift if RPM would be less than max after downshifting. RPMoffset to prevent hunting
            if (currentGear.Value > 1 && (engineRPMPort.Value * ratios[(int)(currentGear.Value - 1)] / ratios[(int)currentGear.Value]) < (Main.settings.maxRPM - Main.settings.RPMoffset))
            {
                changingGears = true;
                endGearChange = false;
                car.StartCoroutine(ChangeGear(-1));
                return;
            }
        }

        internal void StopShift()
        {
            endGearChange = true;
        }

        internal void OnEndControl()
        {
            StopShift();

            //Select the most suitable gear ratio to leave the locomotive in
            if (Main.settings.CVT && reverser.Value != 0.5f && currentGear.Value != 0)
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
                CVTRatio = ratios[(int)currentGear.Value];
            }
            else
            {
                if (!Main.mod.Enabled) return;
                if (reverser.Value == 0.5f || currentGear.Value == 0) return;

                //Find adequate gear for current speed
                float[] speeds = { 17, 25, 35, 45, 62, float.PositiveInfinity };
                int neededGear = Array.FindIndex(speeds, x => Mathf.Abs(wheelSpeedPort.Value) < x) + 1;

                if (neededGear != currentGear.Value)
                {
                    changingGears = true;
                    endGearChange = true; //Skip waiting times
                    car.StartCoroutine(ChangeGear(neededGear - (int)currentGear.Value));
                }
            }
        }

        private IEnumerator ChangeGear(int amount)
        {
            float t = throttle.Value;

            //Set throttle to 0
            if (t != 0) throttle.Set(0);
            //Prevent (undo) throttle changes while shifting
            throttle.ControlUpdated += throttleUpdated;

            //If throttle needed to be moved
            if (t != 0)
            {
                //Wait for shiftDelay
                float time = Time.time;
                while (Time.time < time + Main.settings.shiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            //Change gear
            gearPort.ExternalValueUpdate(Mathf.Clamp(gearPort.Value + amount/6f, 1f/6f, 1));

            //If throttle needed to be moved
            if (t != 0)
            {
                //Wait for shiftDelay
                float time = Time.time;
                while (Time.time < time + Main.settings.shiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            //Stop watching throttle
            throttle.ControlUpdated -= throttleUpdated;
            yield return null; //To prevent a race condition

            //Reset throttle to prevois values + any changes while shifting
            if (t != 0) throttle.Set(t);

            //Wait for postShiftDelay
            {
                float time = Time.time;
                while (Time.time < time + Main.settings.postShiftDelay && !endGearChange)
                {
                    yield return null;
                }
            }

            changingGears = false;

            void throttleUpdated(float value)
            {
                if (value == 0) return;

                //Save value to increase throttle after shifting
                t += value;

                //Wait one frame to reset throttle
                car.StartCoroutine(SetThrottle());
                IEnumerator SetThrottle()
                {
                    yield return null;
                    throttle.Set(0);
                }
            }
        }
    }
}
