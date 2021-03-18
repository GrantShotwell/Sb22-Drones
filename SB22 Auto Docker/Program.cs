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

		IMyTextSurface TextSurface { get; }

		IMyRemoteControl Control { get; set; }
		IMyShipConnector Connector { get; set; }
		Vector3D ControlToConnector => Connector.Position - Control.Position;
		Vector3D ConnectorToControl => Control.Position - Connector.Position;

		List<IMyGyro> Gyroscopes { get; } = new List<IMyGyro>();
		List<IMyThrust> Thrusters { get; } = new List<IMyThrust>();

		bool TargetConnectorExists { get; set; }
		float TargetConnectorHalfLength { get; set; }
		Vector3D TargetConnectorLocalPosition { get; set; }
		Vector3D TargetConnectorWorldPosition { get; set; }
		Quaternion TargetConnectorWorldRotation { get; set; }
		Vector3D TargetConnectorForward { get; set; }
		Vector3D TargetConnectorUp { get; set; }

		double ConnectDistance => TargetConnectorHalfLength + (Base6Directions.GetIntVector(Base6Directions.Direction.Forward) * Connector.Max).AbsMax();
		double ApproachDistance => ConnectDistance + Me.CubeGrid.WorldVolume.Radius * 1.5;

		string DefaultEcho { get; set; }


		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Set update frequency.
			Runtime.UpdateFrequency = UpdateFrequency.None;

			// Get the main text surface of the programmable block.
			TextSurface = Me.GetSurface(0);
			TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
			TextSurface.WriteText("Program initialized.\n");

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

			Echo($"Last execution took {Runtime.LastRunTimeMs:N6}ms.\n\n");
			Echo($"Current update source: {Convert.ToString((int)updateSource, 2).PadLeft(10, '0')}\n");

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
						TextSurface.WriteText("Undocking command recieved.\n", true);
					}

					break;

				case "dock":

					if(Connector.Status != MyShipConnectorStatus.Connected) {
						var data = Communicator.MakeDockRequestMessageData(Connector.GetPosition());
						IGC.SendBroadcastMessage(Communicator.tagDockRequest, data, TransmissionDistance.TransmissionDistanceMax);
						TextSurface.WriteText("Docking request sent. Waiting for reply...\n", true);
					}

					break;

			}

			if((updateSource | UpdateType.IGC) != 0) {

				while(IGC.UnicastListener.HasPendingMessage) {

					MyIGCMessage message = IGC.UnicastListener.AcceptMessage();

					switch(message.Tag) {

						case Communicator.tagDockAccept: {

							Base6Directions.Direction direction;
							Vector3 normal;
							Vector3I up;
							float length;
							if(!Communicator.ParseDockAcceptMessageData(message.Data as string, out length, out direction, out normal, out up)) {
								TextSurface.WriteText("Failed to parse dock accept message.\n", true);
								break;
							}

							GridTerminalSystem.GetBlocksOfType(Gyroscopes, gyroscope => gyroscope.CubeGrid == Me.CubeGrid);
							GridTerminalSystem.GetBlocksOfType(Thrusters, thruster => thruster.CubeGrid == Me.CubeGrid);

							TargetConnectorExists = true;
							TargetConnectorHalfLength = length / 2f;
							TargetConnectorLocalPosition = normal;
							TargetConnectorForward = Base6Directions.GetVector(direction);
							TargetConnectorUp = up;
							TextSurface.WriteText("Docking request accepted. Waiting for update...\n", true);
							break;

						}

						case Communicator.tagDockUpdate: {

							Vector3D gridPosition;
							Quaternion gridRotation;
							if(!Communicator.ParseDockUpdateMessageData(message.Data as string, out gridPosition, out gridRotation)) {
								TextSurface.WriteText("Failed to parse dock update message.\n", true);
								break;
							}

							if(TargetConnectorExists) {

								Runtime.UpdateFrequency = UpdateFrequency.Update1;
								TargetConnectorWorldRotation = gridRotation;
								TargetConnectorWorldPosition = gridPosition + gridRotation * TargetConnectorLocalPosition;

								TextSurface.WriteText("Docking order recieved. Initiating docking procedure.\n", true);
								break;

							} else {

								TextSurface.WriteText("Recieved dock update before the dock order.\n", true);
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
						Runtime.UpdateFrequency = UpdateFrequency.None;
						TextSurface.WriteText("Finished docking.\n");

					} else if(Connector.Status == MyShipConnectorStatus.Connected) {

						TargetConnectorExists = false;
						Runtime.UpdateFrequency = UpdateFrequency.None;
						TextSurface.WriteText("Finished docking.\n");

					} else if(TargetConnectorExists) {

						//TextSurface.WriteText("Docking.");
						//TextSurface.WriteText(DefaultEcho);

						// Rotate to connect.
						Quaternion rotation;
						Connector.Orientation.GetQuaternion(out rotation);
						Vector3 current = rotation * Base6Directions.GetVector(Connector.Orientation.Up);
						NavigationHelper.RotateTo(current, TargetConnectorWorldRotation * TargetConnectorUp, Gyroscopes);

						// Move to connect.
						NavigationHelper.MoveToLocal(TargetConnectorWorldPosition - rotation * Connector.Position, Thrusters);

					}

				}

			}

		}

		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/>or some other means.
		/// </summary>
		public void Save() {

		}

	}

}
