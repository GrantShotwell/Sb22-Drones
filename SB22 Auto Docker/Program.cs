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
	/// 
	/// </summary>
	partial class Program : MyGridProgram {

		ConsoleHelper Console { get; }

		IMyRemoteControl Control { get; set; }
		IMyShipConnector Connector { get; set; }

		List<IMyGyro> Gyroscopes { get; } = new List<IMyGyro>();
		List<IMyThrust> Thrusters { get; } = new List<IMyThrust>();

		bool DockingRequested { get; set; }
		bool TargetConnectorExists { get; set; }
		float TargetConnectorClearance { get; set; }
		Vector3D TargetConnectorWorldPosition { get; set; }
		Quaternion TargetConnectorWorldRotation { get; set; }
		Vector3 TargetConnectorWorldVelocity { get; set; }

		float ConnectDistance => TargetConnectorClearance;
		float ApproachDistance => ConnectDistance + (float)Me.CubeGrid.WorldVolume.Radius * 1.3f;


		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Get the main text surface of the programmable block.
			Console = new ConsoleHelper(Me.GetSurface(0));

			// Load settings from custom data.
			bool storageLoad = true;
			ParseSettings(ref storageLoad);

			// Load from storage.
			if(storageLoad) {
				LoadStorage();
			}

			// Set update frequency.
			Runtime.UpdateFrequency = TargetConnectorExists ? UpdateFrequency.Update1 : UpdateFrequency.None;

			// Finish.
			Console.WriteLine("Program constructed.");
			Console.Apply();

		}

		private void LoadStorage() {

			string storage = Storage;
			string[] elements = storage.Split(',');

			if(elements.Length == 0) {
				return;
			}

			try {

				int i = 0;

				bool requested;
				bool.TryParse(elements[i++], out requested);
				DockingRequested = requested;

				bool exists;
				bool.TryParse(elements[i++], out exists);
				TargetConnectorExists = exists;

				float clearance;
				float.TryParse(elements[i++], out clearance);
				TargetConnectorClearance = clearance;

				Vector3D position;
				double.TryParse(elements[i++], out position.X);
				double.TryParse(elements[i++], out position.Y);
				double.TryParse(elements[i++], out position.Z);
				TargetConnectorWorldPosition = position;

				Quaternion rotation;
				float.TryParse(elements[i++], out rotation.X);
				float.TryParse(elements[i++], out rotation.Y);
				float.TryParse(elements[i++], out rotation.Z);
				float.TryParse(elements[i++], out rotation.W);
				TargetConnectorWorldRotation = rotation;

				Vector3 velocity;
				float.TryParse(elements[i++], out velocity.X);
				float.TryParse(elements[i++], out velocity.Y);
				float.TryParse(elements[i++], out velocity.Z);
				TargetConnectorWorldVelocity = velocity;

				Console.WriteLine("Loaded values from storage.");
				Storage = string.Empty;

			}
			catch(Exception e) {

				Console.WriteLine($"Error loading values from storage. ({elements.Length})");
				Console.WriteLine(e.GetType().Name);

			}

		}

		private void ParseSettings(ref bool storageLoad) {

			string[] settings = Me.CustomData.Split('\n');
			foreach(string setting in settings) {

				if(string.IsNullOrWhiteSpace(setting)) {
					continue;
				}

				string[] split = setting.Split('=');
				if(split.Length != 2) {
					Console.WriteLine("Could not split setting:");
					Console.WriteLine(setting);
					continue;
				}

				string name = split[0].Trim();
				string data = split[1].Trim();

				switch(name) {

					case "storageLoad":
						if(!bool.TryParse(data, out storageLoad)) {
							Console.WriteLine("Could not parse setting data into bool:");
							Console.WriteLine(setting);
						}
						break;

					default:
						Console.WriteLine("Unknown setting:");
						Console.WriteLine(setting);
						break;

				}

			}

		}


		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/>or some other means.
		/// </summary>
		public void Save() {

			string[] elements = new string[] {
				DockingRequested.ToString(),
				TargetConnectorExists.ToString(),
				TargetConnectorClearance.ToString(),
				TargetConnectorWorldPosition.X.ToString(), TargetConnectorWorldPosition.Y.ToString(), TargetConnectorWorldPosition.Z.ToString(),
				TargetConnectorWorldRotation.X.ToString(), TargetConnectorWorldRotation.Y.ToString(), TargetConnectorWorldRotation.Z.ToString(), TargetConnectorWorldRotation.W.ToString(),
				TargetConnectorWorldVelocity.X.ToString(), TargetConnectorWorldVelocity.Y.ToString(), TargetConnectorWorldVelocity.Z.ToString()
			};

			Storage = string.Join(",", elements);

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

			Echo($"Last execution took {Runtime.LastRunTimeMs:N6}ms.");

			bool error = false;
			if(Connector == null)
				error |= TryFindConnector();
			if(Control == null)
				error |= TryFindControl();
			if(error)
				return;

			switch(argument) {

				case "undock":

					if(Connector.Status == MyShipConnectorStatus.Connected) {
						Console.WriteLine("Undocking command recieved.");
					}

					break;

				case "dock":

					if(Connector.Status != MyShipConnectorStatus.Connected) {
						var data = Communicator.MakeDockRequestMessageData(Connector.GetPosition());
						IGC.SendBroadcastMessage(Communicator.tagDockRequest, data, TransmissionDistance.TransmissionDistanceMax);
						Console.WriteLine("Docking request sent. Waiting for reply.");
						DockingRequested = true;
						Runtime.UpdateFrequency = UpdateFrequency.Update10;
					}

					break;

			}

			if(updateSource.HasFlag(UpdateType.IGC))
				UpdateFromAntenna();
			if(updateSource.HasFlag(UpdateType.Update1) || updateSource.HasFlag(UpdateType.Update10) || updateSource.HasFlag(UpdateType.Update100))
				UpdateFromClock();

			Console.Apply();

		}

		private bool TryFindConnector() {

			var connectors = new List<IMyShipConnector>();
			GridTerminalSystem.GetBlocksOfType(connectors, _connector => _connector.CubeGrid == Me.CubeGrid);

			if(connectors.Count == 0) {
				Echo("Cannot find a connector to use.");
				return true;
			} else {
				Connector = connectors[0];
				return false;
			}

		}

		private bool TryFindControl() {

			var controls = new List<IMyRemoteControl>();
			GridTerminalSystem.GetBlocksOfType(controls, _control => _control.CubeGrid == Me.CubeGrid);

			if(controls.Count == 0) {
				Echo("Cannot find a remote control to use.");
				return true;
			} else {
				Control = controls[0];
				return false;
			}

		}

		private void UpdateFromClock() {

			// Stop if there is no target.
			if(!TargetConnectorExists) return;

			// Get properties as local variables.
			IMyRemoteControl control = Control;
			IMyShipConnector connector = Connector;

			// Connect if possible.
			if(connector.Status == MyShipConnectorStatus.Connectable) {

				connector.Connect();
				FinishedDocking();
				return;

			} else if(connector.Status == MyShipConnectorStatus.Connected) {

				FinishedDocking();
				return;

			}

			// Rotate to connect.
			Quaternion offset;
			connector.Orientation.GetQuaternion(out offset);
			Quaternion current = Quaternion.CreateFromRotationMatrix(Me.CubeGrid.WorldMatrix);
			Quaternion target = TargetConnectorWorldRotation;
			bool doneRotate = NavigationHelper.RotateTo(
				grid: current,
				target: target * offset,
				control: control,
				gyroscopes: Gyroscopes,
				speed: 2f,
				echo: null
			);

			// Move to connect.
			bool doneMove = NavigationHelper.MoveTo(
				current: connector.GetPosition(),
				target: TargetConnectorWorldPosition + TargetConnectorWorldRotation * (Vector3.Forward * ApproachDistance),
				velocity: TargetConnectorWorldVelocity,
				thrusters: Thrusters,
				control: control,
				speed: 50f,
				echo: Echo
			);

		}

		private void FinishedDocking() {
			TargetConnectorExists = false;
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
			Console.WriteLine("Finished docking.");
		}

		private void UpdateFromAntenna() {

			while(IGC.UnicastListener.HasPendingMessage) {
				AcceptNextMessage();
			}

		}

		private void AcceptNextMessage() {

			MyIGCMessage message = IGC.UnicastListener.AcceptMessage();
			switch(message.Tag) {
				case Communicator.tagDockAccept:
					DockAccept(message);
					break;
				case Communicator.tagDockUpdate:
					DockUpdate(message);
					break;
			}

		}

		private void DockUpdate(MyIGCMessage message) {

			// Parse the message data using my communication library.
			// Stop if parsing failed.
			Vector3D connectorPosition;
			Quaternion connectorRotation;
			Vector3 connectorVelocity;
			if(!Communicator.ParseDockUpdateMessageData(message.Data as string, out connectorPosition, out connectorRotation, out connectorVelocity)) {
				Console.WriteLine("Failed to parse dock update message.");
				Console.WriteLine(message.Data);
				return;
			}

			// Stop if target does not exist.
			if(!TargetConnectorExists) {
				Console.WriteLine("Recieved dock update before the dock order.");
				return;
			}

			Runtime.UpdateFrequency = UpdateFrequency.Update1;
			UpdateTargetConnector(connectorPosition, connectorRotation, connectorVelocity);

		}

		private void DockAccept(MyIGCMessage message) {

			float clearance;
			if(!Communicator.ParseDockAcceptMessageData(message.Data as string, out clearance)) {
				Console.WriteLine("Failed to parse dock accept message.");
				Console.WriteLine(message.Data);
			}

			GridTerminalSystem.GetBlocksOfType(Gyroscopes, gyroscope => gyroscope.CubeGrid == Me.CubeGrid);
			GridTerminalSystem.GetBlocksOfType(Thrusters, thruster => thruster.Enabled && thruster.CubeGrid == Me.CubeGrid);

			TargetConnectorExists = true;
			TargetConnectorClearance = clearance;
			Console.WriteLine("Docking request accepted.");

		}

		private void UpdateTargetConnector(Vector3D position, Quaternion rotation, Vector3 velocity) {
			TargetConnectorWorldRotation = rotation;
			TargetConnectorWorldPosition = position;
			TargetConnectorWorldVelocity = velocity;
		}

	}

}
