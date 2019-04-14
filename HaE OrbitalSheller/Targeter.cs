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
        public class Targeter
        {
            public double speedCap;
            public double launchVelocity;
            public double tolerance = 75;
            public IMyShipController reference;

            public double TargetingProgress => simTargeting?.GetLastMissDistance() ?? 0;

            public Action<Vector3D> directionFoundCallback;

            private Simulated_Targeting simTargeting;
            private bool fireCallbackOnce = false;

            public Targeter(double speedCap, double launchVelocity, IMyShipController reference)
            {
                this.speedCap = speedCap;
                this.launchVelocity = launchVelocity;
                this.reference = reference;
            }

            public void TargetPosition(Vector3D position, MyDetectedEntityInfo planet)
            {
                Vector3D startPos = reference.GetPosition();
                Vector3D direction = Vector3D.Normalize(position - startPos);

                simTargeting = new Simulated_Targeting(reference, position, startPos, direction, 0, planet, 9.81, launchVelocity, speedCap);
                simTargeting.tolerance = tolerance;

                fireCallbackOnce = false;
            }

            public void TargetPosition(Vector3D position, Vector3D planetCenter, double planetRadius, double gravity = 9.81)
            {
                Vector3D startPos = reference.GetPosition();
                Vector3D direction = Vector3D.Normalize(position - startPos);

                simTargeting = new Simulated_Targeting(reference, position, startPos, direction, 0, planetCenter, planetRadius, gravity, launchVelocity, speedCap);
                simTargeting.tolerance = tolerance;

                fireCallbackOnce = false;
            }

            public void TargetingLoop()
            {
                if (simTargeting?.Calculate() ?? false && !fireCallbackOnce)
                {
                    directionFoundCallback?.Invoke(simTargeting.GetCurrentFiringDirection());
                    fireCallbackOnce = true;
                }
            }
        }
    }
}
