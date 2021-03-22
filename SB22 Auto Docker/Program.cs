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

		float ConnectDistance => TargetConnectorClearance;
		float ApproachDistance => ConnectDistance + (float)Me.CubeGrid.WorldVolume.Radius * 1.3f;


		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Get the main text surface of the programmable block.
			Console = new ConsoleHelper(Me.GetSurface(0));

			// Load from storage.
			string storage = Storage;
			string[] elements = storage.Split(',');
			if(elements.Length > 0) {
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

					Console.WriteLine("Loaded values from storage.");

				} catch(Exception e) {

					Console.WriteLine("Error loading values from storage.");
					Console.WriteLine(e.GetType().Name);

				}
			}

			// Set update frequency.
			Runtime.UpdateFrequency = TargetConnectorExists ? UpdateFrequency.Update1 :  UpdateFrequency.None;

			// Finish.
			Console.WriteLine("Program constructed.");
			Console.Apply();

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
				TargetConnectorWorldRotation.X.ToString(), TargetConnectorWorldRotation.Y.ToString(), TargetConnectorWorldRotation.Z.ToString(), TargetConnectorWorldRotation.W.ToString()
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

			if(Connector == null) {
				var connectors = new List<IMyShipConnector>();
				GridTerminalSystem.GetBlocksOfType(connectors, _connector => _connector.CubeGrid == Me.CubeGrid);
				if(connectors.Count == 0) {
					Echo("Cannot find a connector to use.");
					error = true;
				} else Connector = connectors[0];
			}

			if(Control == null) {
				var controls = new List<IMyRemoteControl>();
				GridTerminalSystem.GetBlocksOfType(controls, _control => _control.CubeGrid == Me.CubeGrid);
				if(controls.Count == 0) {
					Echo("Cannot find a remote control to use.");
					error = true;
				} else Control = controls[0];
			}

			if(error) return;

			IMyRemoteControl control = Control;
			IMyShipConnector connector = Connector;

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

			if((updateSource | UpdateType.IGC) != 0) {

				while(IGC.UnicastListener.HasPendingMessage) {

					MyIGCMessage message = IGC.UnicastListener.AcceptMessage();

					switch(message.Tag) {

						case Communicator.tagDockAccept: {

							float clearance;
							if(!Communicator.ParseDockAcceptMessageData(message.Data as string, out clearance)) {
								Console.WriteLine("Failed to parse dock accept message.");
								Console.WriteLine(message.Data);
								break;
							}

							GridTerminalSystem.GetBlocksOfType(Gyroscopes, gyroscope => gyroscope.CubeGrid == Me.CubeGrid);
							GridTerminalSystem.GetBlocksOfType(Thrusters, thruster => thruster.CubeGrid == Me.CubeGrid);

							TargetConnectorExists = true;
							TargetConnectorClearance = clearance;
							Console.WriteLine("Docking request accepted.");
							break;

						}

						case Communicator.tagDockUpdate: {

							Vector3D connectorPosition;
							Quaternion connectorRotation;
							if(!Communicator.ParseDockUpdateMessageData(message.Data as string, out connectorPosition, out connectorRotation)) {
								Console.WriteLine("Failed to parse dock update message.");
								Console.WriteLine(message.Data);
								break;
							}

							if(TargetConnectorExists) {

								Runtime.UpdateFrequency = UpdateFrequency.Update1;
								TargetConnectorWorldRotation = connectorRotation;
								TargetConnectorWorldPosition = connectorPosition;
								break;

							} else {

								Console.WriteLine("Recieved dock update before the dock order.");
								break;

							}

						}

						default:
							break;

					}

				}

			}

			if((updateSource | UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100) != 0) {

				if(TargetConnectorExists) {

					// Connect if possible.
					if(Connector.Status == MyShipConnectorStatus.Connectable) {

						Connector.Connect();
						TargetConnectorExists = false;
						Runtime.UpdateFrequency = UpdateFrequency.Update100;
						Console.WriteLine("Finished docking.");

					} else if(Connector.Status == MyShipConnectorStatus.Connected) {

						TargetConnectorExists = false;
						Runtime.UpdateFrequency = UpdateFrequency.Update100;
						Console.WriteLine("Finished docking.");

					} else if(TargetConnectorExists) {

						// Rotate to connect.
						Quaternion offset;
						Connector.Orientation.GetQuaternion(out offset);
						Quaternion current = Quaternion.CreateFromRotationMatrix(Me.CubeGrid.WorldMatrix);
						Quaternion target = TargetConnectorWorldRotation;
						NavigationHelper.RotateTo(current, target * Quaternion.Inverse(offset), control, Gyroscopes);

						// Move to connect.
						NavigationHelper.MoveTo(Connector.GetPosition(), TargetConnectorWorldPosition + TargetConnectorWorldRotation * (Vector3.Forward * ApproachDistance), Thrusters, control, 1.0, Echo);

					}

				}

			}

			Console.Apply();

		}

	}

}
