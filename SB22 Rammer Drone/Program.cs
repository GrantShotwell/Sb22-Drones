using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using Sb22.ScriptHelpers;
using Sb22.Drones;

namespace IngameScript {

	/// <summary>
	/// Rammer Drone
	/// </summary>
	partial class Program : MyGridProgram {

		double RerouteDistanceSquared { get; } = 25.0;
		double TimeSinceLastReroute { get; set; } = 999.9;
		double MinimumRerouteTime { get; } = 0.2;

		string TurretName { get; } = "Drone Turret";
		IMyLargeTurretBase Turret { get; set; }

		string RemoteControlName { get; } = "Drone Remote Control";
		IMyRemoteControl RemoteControl { get; set; }

		List<IMyThrust> ForwardThrusters { get; } = new List<IMyThrust>();
		List<IMyThrust> BackwardThrusters { get; } = new List<IMyThrust>();


		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Set update frequency.
			Runtime.UpdateFrequency = UpdateFrequency.Update10;

		}


		/// <summary>
		/// The main entry point of the script,
		/// invoked every time one of the programmable block's Run actions are invoked,
		/// or the script updates itself.
		/// </summary>
		/// <param name="argument">Argument specified by the caller.</param>
		/// <param name="updateSource">A bitfield, possibly with more than one value,
		/// that describes where the update came from.</param>
		public void Main(string argument, UpdateType updateSource) {

			Echo($"Last execution took {Runtime.LastRunTimeMs:N6}ms.\n");


			bool errors = false;

			// Update turret block.
			if(Turret == null || Turret.Name != TurretName)
				Turret = GridTerminalSystem.GetBlockWithName(TurretName) as IMyLargeTurretBase;
			if(Turret == null) {
				errors = true;
				Echo($"Could not find a turret with the name '{TurretName}' to use for targeting.");
			}

			// Update remote control block.
			if(RemoteControl == null || RemoteControl.Name != RemoteControlName)
				RemoteControl = GridTerminalSystem.GetBlockWithName(RemoteControlName) as IMyRemoteControl;
			if(RemoteControl == null) {
				errors = true;
				Echo($"Could not find a remote control with the name '{RemoteControlName}' to use for navigating.");
			}

			// End of error checking.
			if(errors) return;


			// Make proerties local.
			IMyRemoteControl control = RemoteControl;
			IMyLargeTurretBase turret = Turret;

			// Navigation logic.
			if(Turret.HasTarget) {

				Vector3D current = control.CurrentWaypoint.Coords;
				MyDetectedEntityInfo target = turret.GetTargetedEntity();
				Vector3D velocity = target.Velocity;

				bool notWaitingForTime, notWaitingforDist = true;
				double dist = -1;
				if(
					(notWaitingForTime = (TimeSinceLastReroute += Runtime.TimeSinceLastRun.TotalSeconds) > MinimumRerouteTime)
					&& (notWaitingforDist = (dist = (current - target.Velocity + target.Position).LengthSquared()) > RerouteDistanceSquared)
				) {

					// Find where to aim.
					Vector3D aim;
					Vector3D position = control.GetPosition();
					Vector3D distance = target.Position - position;

					double d2 = distance.LengthSquared();
					double d = Math.Sqrt(d2);
					double v2 = velocity.LengthSquared();

					if(v2 == 0.0) {
						aim = target.Position;
					} else {
						double v = Math.Sqrt(v2);
						double s = control.SpeedLimit;
						double s2 = s * s;
						double x2 = (distance + velocity).LengthSquared();
						double cos_a = (v2 + d2 - x2) / (2 * v * d);
						double cos2_a = cos_a * cos_a;
						double t = d * (Math.Sqrt(v2 * cos2_a + 4 * s2 - 4 * v2) + v * cos_a) / (2 * (s2 - v2));
						aim = double.IsNaN(t) ? current : velocity * t + target.Position;
					}

					// Tell remote control to go to the aim.
					control.ClearWaypoints();
					control.AddWaypoint(aim, "Aim");
					TimeSinceLastReroute = 0;

				}

				// Enable autopilot.
				Runtime.UpdateFrequency = UpdateFrequency.Update10;
				control.SetCollisionAvoidance(false);
				control.FlightMode = FlightMode.OneWay;
				control.ControlThrusters = false;
				control.ControlWheels = true;
				control.HandBrake = false;
				control.SetAutoPilotEnabled(true);

				// Set thruster overrides.
				SetThrusterOverrides(1.00f, 0.00f);

				// Echo information.
				Echo($"\nRamming target!");
				if(!notWaitingForTime) Echo($"Reroute waiting for time ({TimeSinceLastReroute:N2}s).");
				if(!notWaitingforDist) Echo($"Reroute waiting for distance ({Math.Sqrt(dist):N2}m).");

			} else {

				// Disable autopilot.
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
				control.SetAutoPilotEnabled(false);
				control.Direction = Base6Directions.Direction.Forward;

				// Set thruster overrides.
				UpdateThrusterCollections();
				SetThrusterOverrides(0.00f, 0.00f);

				// Echo information.
				Echo("\nWaiting for target...");

			}

		}

		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/>or some other means.
		/// </summary>
		public void Save() {
			Storage = null;
		}

		void SetThrusterOverrides(float forwards, float backwards) {

			// Set thruster overrides.
			foreach(IMyThrust thruster in BackwardThrusters) thruster.ThrustOverridePercentage = backwards;
			foreach(IMyThrust thruster in ForwardThrusters) thruster.ThrustOverridePercentage = forwards;

			// Echo information.
			Echo($"Thrusters count to {ForwardThrusters.Count} forwards and {BackwardThrusters.Count} backwards.");
			Echo($"Overrides set to {forwards:P1}/{backwards:P1}.");

		}

		void UpdateThrusterCollections() {

			// Get remote control.
			var control = RemoteControl;
			if(control == null) return;

			// Find backward thrusters.
			Base6Directions.Direction orientation = DirectionHelper.Apply(control.Orientation.Forward, control.Direction);
			List<IMyThrust> backwardThrusters = BackwardThrusters;
			GridTerminalSystem.GetBlocksOfType(backwardThrusters,
				thruster => thruster.CubeGrid == Me.CubeGrid && thruster.Orientation.Forward == orientation);

			// Find forward thrusters.
			DirectionHelper.Invert(ref orientation);
			List<IMyThrust> forwardThrusters = ForwardThrusters;
			GridTerminalSystem.GetBlocksOfType(forwardThrusters,
				thruster => thruster.CubeGrid == Me.CubeGrid && thruster.Orientation.Forward == orientation);

		}

	}
}
