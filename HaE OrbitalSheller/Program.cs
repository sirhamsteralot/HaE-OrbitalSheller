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
            IMyShipController reference = GridTerminalSystem.GetBlockWithName((string)cannonSettings.GetValue("referenceName")) as IMyShipController;

            yield return true;
            targeter = new Targeter
                (
                    (double)cannonSettings.GetValue("speedCap"),
                    (double)cannonSettings.GetValue("launchVelocity"),
                    reference
                );
            targeter.directionFoundCallback += TargetCalculatedCallback;

            yield return true;


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