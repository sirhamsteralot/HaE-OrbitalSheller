using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        Scheduler scheduler;
        INISerializer cannonSettings;
        IngameTime ingameTime;
        GridTerminalSystemUtils GTSUtils;

        Cannon cannon;
        Targeter targeter;

        bool initialized = false;

        public Program()
        {
            scheduler = new Scheduler();
            scheduler.AddTask(Init());
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update10;
        }

        public IEnumerator<bool> Init()
        {
            cannonSettings = new INISerializer("CannonSettings");
            cannonSettings.AddValue("referenceName", x => x, "RCReference");
            cannonSettings.AddValue("speedCap", x => double.Parse(x), 104.38);
            cannonSettings.AddValue("launchVelocity", x => double.Parse(x), 100.0);
            cannonSettings.AddValue("sourceRotorTName", x => x, "[OrbitalCannonBase]_[Azimuth]");
            cannonSettings.AddValue("elevationTag", x => x, "[Elevation]");
            cannonSettings.AddValue("timerName", x => x, "CannonTimer");


            if (Me.CustomData == "")
            {
                string temp = Me.CustomData;
                cannonSettings.FirstSerialization(ref temp);
                Me.CustomData = temp;
            }
            else
            {
                cannonSettings.DeSerialize(Me.CustomData);
            }
            yield return true;
            ingameTime = new IngameTime();
            GTSUtils = new GridTerminalSystemUtils(Me, GridTerminalSystem);

            yield return true;
            IMyShipController reference = GridTerminalSystem.GetBlockWithName((string)cannonSettings.GetValue("referenceName")) as IMyShipController;
            IMyMotorStator sourceRotor = GridTerminalSystem.GetBlockWithName((string)cannonSettings.GetValue("sourceRotorTName")) as IMyMotorStator;
            IMyTimerBlock timer = GridTerminalSystem.GetBlockWithName((string)cannonSettings.GetValue("timerName")) as IMyTimerBlock;

            Echo($"Getting blocks status:...\nreference: {reference != null}\nsourceRotor: {sourceRotor != null}");

            if (reference == null || sourceRotor == null)
                throw new Exception("cant get blocks!");

            yield return true;
            targeter = new Targeter
                (
                    (double)cannonSettings.GetValue("speedCap"),
                    (double)cannonSettings.GetValue("launchVelocity"),
                    reference
                );
            targeter.directionFoundCallback += TargetCalculatedCallback;

            yield return true;

            cannon = Cannon.CreateCannon(sourceRotor, GTSUtils, ingameTime, reference, (string)cannonSettings.GetValue("sourceRotorTName"), (string)cannonSettings.GetValue("elevationTag"));
            cannon.Timer = timer;
            yield return true;
            Echo("Initialized!");
            initialized = true;
        }


        public void Main(string argument, UpdateType updateSource)
        {
            scheduler.Main();

            if (!initialized)
                return;

            HandleCommands(argument);

            if ((updateSource & UpdateType.Update1) != 0)
            {
                ingameTime.Tick();
                targeter.TargetingLoop();
                cannon.Tick();
            }

            if ((updateSource & UpdateType.Update10) != 0)
            {
                Me.GetSurface(0).WriteText($"{targeter.TargetingProgress}");
            }
        }

        public void HandleCommands(string argument)
        {
            if (argument.StartsWith("TargetPosition"))
                TargetPosition(argument);

            if (argument.StartsWith("Reset"))
                Reset();

        }
        
        public void Reset()
        {
            Echo("Resetting...");
            scheduler.AddTask(Init());
        }

        public void TargetPosition(string args)
        {
            string[] split = args.Split('|');

            if (split.Length < 4)
            {
                Echo("Invalid Argument");
                return;
            }

            Vector3D targetPosition;
            if (!Vector3D.TryParse(split[1], out targetPosition))
            {
                Echo("Invalid Position");
                return;
            }

            Vector3D planetPosition;
            if (!Vector3D.TryParse(split[2], out planetPosition))
            {
                Echo("Invalid Planet Position");
                return;
            }
            cannon.EnterIdle();
            double planetRadius;
            if (!double.TryParse(split[3], out planetRadius))
            {
                Echo("Invalid Radius");
                return;
            }

            double tolerance = 10;
            if (split.Length > 4 && !double.TryParse(split[4], out tolerance))
            {
                Echo("Invalid tolerance");
                return;
            }

            double tweakFactor = 1;
            if (split.Length > 5 && !double.TryParse(split[5], out tweakFactor))
            {
                Echo("Invalid tweakFactor");
                return;
            }

            targeter.tolerance = tolerance;
            targeter.TargetPosition(targetPosition, planetPosition, planetRadius, 9.81 * tweakFactor);
            Echo($"Targeting...\nTarget: {targetPosition}\nplanetCore: {planetPosition}\nplanetRadius: {planetRadius}\ntweakFactor: {tweakFactor}");
        }

        public void TargetCalculatedCallback(Vector3D direction)
        {
            cannon.TargetDirection(ref direction);
        }
    }
}