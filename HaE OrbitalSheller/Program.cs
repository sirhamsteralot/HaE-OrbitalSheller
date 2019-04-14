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

        public Program()
        {
            scheduler = new Scheduler();
            scheduler.AddTask(Init());
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public IEnumerator<bool> Init()
        {
            cannonSettings = new INISerializer("CannonSettings");
            cannonSettings.AddValue("referenceName", x => x, "RCReference");
            cannonSettings.AddValue("speedCap", x => double.Parse(x), "RCReference");
            cannonSettings.AddValue("launchVelocity", x => double.Parse(x), "RCReference");
            cannonSettings.AddValue("sourceRotorTName", x => x, "[OrbitalCannonBase] [Azimuth]");


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

            yield return true;
            targeter = new Targeter
                (
                    (double)cannonSettings.GetValue("speedCap"),
                    (double)cannonSettings.GetValue("launchVelocity"),
                    reference
                );
            targeter.directionFoundCallback += TargetCalculatedCallback;

            yield return true;

            cannon = Cannon.CreateCannon(sourceRotor, GTSUtils, ingameTime, reference, "[Azimuth]", "[Elevation]");
        }


        public void Main(string argument, UpdateType updateSource)
        {
            targeter.TargetingLoop();
        }

        public void TargetCalculatedCallback(Vector3D position)
        {

        }
    }
}