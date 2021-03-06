﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
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
        public class RotorTurretGroup : ITurretGroup
        {
            private const double accuracyVal = 0.9999;

            public Vector3D defaultDir;
            public bool restMode = false;
            public bool inactive = false;
            public bool restAfterReset = true;
            public bool fireOnce = false;
            public IMyTimerBlock timer;

            Vector3D currentTargetDir = Vector3D.Zero;

            INISerializer turretConfig;
            public double azimuthMultiplier { get { return (double)turretConfig.GetValue("azimuthMultiplier"); } }
            public double elevationMultiplier { get { return (double)turretConfig.GetValue("elevationMultiplier"); } }
            public string referenceName { get { return (string)turretConfig.GetValue("referenceName"); } }

            DeadzoneProvider deadzoneProvider;
            RotorControl rotorControl;
            IngameTime ingameTime;

            


            public RotorTurretGroup(List<IMyMotorStator> rotors, IngameTime ingameTime, DeadzoneProvider deadzoneProvider, string azimuthTag, string elevationTag, GridTerminalSystemUtils GTS)
            {
                Setup(rotors, ingameTime, deadzoneProvider, azimuthTag, elevationTag, GTS);
            }

            public RotorTurretGroup(IMyBlockGroup turretGroup, IngameTime ingameTime, DeadzoneProvider deadzoneProvider, string azimuthTag, string elevationTag, GridTerminalSystemUtils GTS)
            {
                var rotors = new List<IMyMotorStator>();
                turretGroup.GetBlocksOfType(rotors);

                Setup(rotors, ingameTime, deadzoneProvider, azimuthTag, elevationTag, GTS);
            }

            public void Setup(List<IMyMotorStator> rotors, IngameTime ingameTime, DeadzoneProvider deadzoneProvider, string azimuthTag, string elevationTag, GridTerminalSystemUtils GTS)
            {
                this.deadzoneProvider = deadzoneProvider;
                this.ingameTime = ingameTime;

                List<IMyMotorStator> elevation = rotors.Where(x => x.CustomName.Contains(elevationTag)).ToList();
                IMyMotorStator azimuth = rotors.First(x => x.CustomName.Contains(azimuthTag));
                defaultDir = azimuth.WorldMatrix.Forward;

                #region configuration
                turretConfig = new INISerializer("TurretConfig");

                turretConfig.AddValue("azimuthMultiplier", x => double.Parse(x), -1.0);
                turretConfig.AddValue("elevationMultiplier", x => double.Parse(x), -1.0);
                turretConfig.AddValue("referenceName", x => x, "TurretReference");

                if (azimuth.CustomData == "")
                {
                    string temp = azimuth.CustomData;
                    turretConfig.FirstSerialization(ref temp);
                    azimuth.CustomData = temp;
                }
                else
                {
                    turretConfig.DeSerialize(azimuth.CustomData);
                }
                #endregion

                IMyTerminalBlock cannonref = GTS.GridTerminalSystem.GetBlockWithName(referenceName);

                if (cannonref == null)
                    throw new Exception($"referenceName ({referenceName}) resolves to NULL!");

                List<RotorControl.RotorReferencePair> elevationPairs = new List<RotorControl.RotorReferencePair>();
                for (int i = 0; i < elevation.Count; i++)
                {
                    elevationPairs.Add(new RotorControl.RotorReferencePair { rotor = elevation[i], reference = cannonref });
                }

                RotorControl.RotorReferencePair azimuthPair = new RotorControl.RotorReferencePair { rotor = azimuth, reference = cannonref };

                rotorControl = new RotorControl(azimuthPair, elevationPairs);
                rotorControl.onTarget += OnTarget;
                rotorControl.useForward = true;
            }

            public void Tick()
            {
                if (restMode)
                    return;

                if (inactive)
                    return;

                if (currentTargetDir == Vector3D.Zero)
                {
                    rotorControl.AimAtTarget(ref defaultDir, azimuthMultiplier, elevationMultiplier);
                    restAfterReset = true;
                    fireOnce = true;
                    return;
                }

                rotorControl.AimAtTarget(ref currentTargetDir, azimuthMultiplier, elevationMultiplier);
            }

            public void DisableTurret(bool value)
            {
                inactive = value;
                rotorControl.azimuth.rotor.TargetVelocityRad = 0;
                foreach (var rotor in rotorControl.elevationRotors)
                {
                    rotor.rotor.TargetVelocityRad = 0;
                }

                rotorControl.Lock(value);
            }

            public TurretGroupUtils.TurretGroupStatus CheckGroupStatus()
            {
                var damageAmount = TurretGroupUtils.TurretGroupStatus.Active;

                if (rotorControl.azimuth.rotor.IsClosed())
                    return TurretGroupUtils.TurretGroupStatus.MajorDMG;
                if (rotorControl.azimuth.reference.IsClosed())
                    return TurretGroupUtils.TurretGroupStatus.MajorDMG;

                foreach (var elevationRot in rotorControl.elevationRotors)
                {
                    if (elevationRot.rotor.IsClosed())
                        return TurretGroupUtils.TurretGroupStatus.MajorDMG;

                    if (elevationRot.reference.IsClosed())
                        damageAmount = TurretGroupUtils.TurretGroupStatus.MinorDMG;
                }

                return damageAmount;
            }

            public void TargetDirection(ref Vector3D targetDirection)
            {
                if (currentTargetDir != Vector3D.Zero && targetDirection != Vector3D.Zero && currentTargetDir.Dot(targetDirection) < 1 - accuracyVal)
                    return;

                //bool blockInTheWay = deadzoneProvider.IsBlockInTheWay(
                //    rotorControl.azimuth.reference.GetPosition() + targetDirection * 15,
                //    rotorControl.azimuth.reference.GetPosition() + targetDirection * 800,
                //    rotorControl.azimuth.rotor);
                //if (blockInTheWay)
                //{
                //    currentTargetDir = Vector3D.Zero;
                //    restAfterReset = true;
                //    return;
                //}

                currentTargetDir = targetDirection;
                restMode = false;
                restAfterReset = false;
            }

            public void TargetPosition(ref Vector3D position)
            {
                Vector3D targetdir = Vector3D.Normalize(position - rotorControl.azimuth.rotor.GetPosition());
                TargetDirection(ref targetdir);
            }

            private void FireCannons(bool fire)
            {
                if (!fire)
                    return;

                restMode = false;
                restAfterReset = false;

                FireCallback();
            }

            private void FireCallback()
            {
                if (!fireOnce)
                    return;

                timer?.Trigger();
                fireOnce = false;
            }

            private void OnTarget(bool val, RotorControl.RotorReferencePair pair)
            {
                if (val)
                {
                    if (currentTargetDir != Vector3D.Zero)
                    {
                        bool blockInTheWay = deadzoneProvider.IsBlockInTheWay(rotorControl.azimuth.reference.GetPosition() + currentTargetDir * 15, rotorControl.azimuth.reference.GetPosition() + currentTargetDir * 800);
                        FireCannons(!blockInTheWay);
                        rotorControl.Lock(false);
                        return;
                    }
                    
                    if (rotorControl.currentAccuracy > accuracyVal && restAfterReset)
                    {
                        rotorControl.Lock(true);
                        restMode = true;
                    }
                        
                } else if (currentTargetDir != Vector3D.Zero)
                {
                    rotorControl.Lock(false);
                }
            }

            private List<IMyMotorStator> Select(List<IMyMotorStator> elevation)
            {
                var tmplist = new List<IMyMotorStator>();

                for(int i = 0; i < elevation.Count; i++)
                {
                    var tmpRot = RotorLauncher.Selector(elevation[i].TopGrid);
                    if (tmpRot != null)
                        tmplist.Add(tmpRot);
                    else
                        elevation.RemoveAt(i);
                }
                return tmplist;
            }
        }
    }
}
