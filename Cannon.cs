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
    partial class Program
    {
        public class Cannon
        {
            RotorTurretGroup rotorTurretGroup;
            DeadzoneProvider deadzoneProvider;

            IMyShipController reference;

            public IMyTimerBlock Timer { get { return rotorTurretGroup.timer; } set { rotorTurretGroup.timer = value; } }


            public static Cannon CreateCannon
                (
                    IMyMotorStator sourceRotor, GridTerminalSystemUtils GTS, 
                    IngameTime ingameTime, IMyShipController control, 
                    string azimuthTag, string elevationTag
                )
            {
                List<IMyMotorStator> rotors = new List<IMyMotorStator>();
                List<IMyMotorStator> cache = new List<IMyMotorStator>();
                List<IMyMotorStator> prevTop = new List<IMyMotorStator>();
                List<IMyMotorStator> currentTop = new List<IMyMotorStator>();

                DeadzoneProvider deadzoneProvider = new DeadzoneProvider(GTS);

                rotors.Add(sourceRotor);
                prevTop.AddRange(rotors);

                while (prevTop.Count > 0)
                {
                    foreach (var rotor in prevTop)
                    {
                        cache.Clear();
                        rotor.TopGrid?.GetCubesOfType(GTS.GridTerminalSystem, cache);
                        currentTop.AddRange(cache);
                    }

                    rotors.AddRange(currentTop);

                    prevTop.Clear();
                    prevTop.AddRange(currentTop);
                    currentTop.Clear();
                }

                var turretGroup = new RotorTurretGroup(rotors, ingameTime, deadzoneProvider, azimuthTag, elevationTag, GTS);
                turretGroup.TargetDirection(ref Vector3D.Zero);
                turretGroup.defaultDir = control.WorldMatrix.Forward;

                if (turretGroup.CheckGroupStatus() != TurretGroupUtils.TurretGroupStatus.MajorDMG)
                    return new Cannon(turretGroup, deadzoneProvider, control);

                return null;
            }

            public Cannon(List<IMyMotorStator> rotors, IMyShipController reference, IngameTime ingameTime, GridTerminalSystemUtils GTSUtils, string azimuthTag, string elevationTag)
            {
                this.reference = reference;
                deadzoneProvider = new DeadzoneProvider(GTSUtils);
                rotorTurretGroup = new RotorTurretGroup(rotors, ingameTime, deadzoneProvider, azimuthTag, elevationTag, GTSUtils);
            }

            public Cannon(RotorTurretGroup rotorTurretGroup, DeadzoneProvider deadzoneProvider, IMyShipController reference)
            {
                this.rotorTurretGroup = rotorTurretGroup;
                this.deadzoneProvider = deadzoneProvider;
                this.reference = reference;
            }

            public void Tick()
            {
                rotorTurretGroup.Tick();
            }

            public void TargetPosition(ref Vector3D position)
            {
                rotorTurretGroup.TargetPosition(ref position);
            }

            public void TargetDirection(ref Vector3D position)
            {
                rotorTurretGroup.TargetDirection(ref position);
            }

            public void EnterIdle()
            {
                rotorTurretGroup.TargetDirection(ref Vector3D.Zero);
                rotorTurretGroup.defaultDir = reference.WorldMatrix.Forward;
                //rotorTurretGroup.restAfterReset = true;
            }
        }
    }
}
