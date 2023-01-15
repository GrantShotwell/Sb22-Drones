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

		string TurretName { get; } = "Drone Turret";
		IMyLargeTurretBase Turret { get; set; }

		string RemoteControlName { get; } = "Drone Remote Control";
		IMyRemoteControl RemoteControl { get; set; }

		List<IMyThrust> Thrusters { get; } = new List<IMyThrust>();
		List<IMyGyro> Gyroscopes { get; } = new List<IMyGyro>();


		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Set update frequency.
			Runtime.UpdateFrequency = UpdateFrequency.Update1;

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
			if(errors) {
				NavigationHelper.RemoveOverride(Thrusters);
				NavigationHelper.RemoveOverride(Gyroscopes);
				return;
			}


			// Make proerties local.
			// Done here and not before 'error checking'.
			GetObjects();
			IMyRemoteControl control = RemoteControl;
			IMyLargeTurretBase turret = Turret;

			// Navigation logic.
			if(turret.HasTarget) {

				// Echo current status.
				Echo($"\nRamming target!");

				// Get target entity.
				MyDetectedEntityInfo target = turret.GetTargetedEntity();
				Vector3D targetPosition = target.Position;

				// Get information required to move.
				Vector3D direction = control.GetPosition() - targetPosition;
				direction.Normalize();
				Quaternion rotation = Quaternion.CreateFromRotationMatrix(Me.CubeGrid.WorldMatrix);
				Quaternion rotationTo = Quaternion.CreateFromForwardUp(direction, rotation.Up);

				// Rotate ship to face target.
				NavigationHelper.RotateTo(
					grid: rotation,
					target: rotationTo,
					gyroscopes: Gyroscopes,
					speed: 2f,
					echo: Echo
				);

				// Move ship towards target.
				NavigationHelper.MoveTo(
					current: control.GetPosition(),
					target: targetPosition,
					velocity: target.Velocity,
					thrusters: Thrusters,
					control: control,
					slow: false,
					echo: Echo
				);

			} else {

				// Echo current status.
				Echo("\nWaiting for target...");
				NavigationHelper.RemoveOverride(Thrusters);
				NavigationHelper.RemoveOverride(Gyroscopes);

			}

		}

		public void GetObjects() {
			GridTerminalSystem.GetBlocksOfType(Thrusters);
			GridTerminalSystem.GetBlocksOfType(Gyroscopes);
		}

		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/>or some other means.
		/// </summary>
		public void Save() {
			Storage = null;
		}

	}
}
