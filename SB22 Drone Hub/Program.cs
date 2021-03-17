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

		long RuntimeStartNs { get; set; }
		long CurrentRuntimeNs => DateTime.Now.Ticks - RuntimeStartNs;
		long TargetRuntimeNs { get; } = (long)1e6 / 10;

		Dictionary<IMyGridTerminalSystem, List<IMyProgrammableBlock>> Drones { get; }
		IEnumerable<MyIGCMessage> QueuedMessages {
			get {
				while(IGC.UnicastListener.HasPendingMessage)
					yield return IGC.UnicastListener.AcceptMessage();
			}
		}

		private readonly List<IMyShipConnector> connectors = new List<IMyShipConnector>();
		List<IMyShipConnector> Connectors {
			get {
				GridTerminalSystem.GetBlocksOfType(connectors,
					connector => connector.Name.Contains("Dock"));
				return connectors;
			}
		}

		ICollection<DockingDrone> DockingDrones { get; } = new LinkedList<DockingDrone>();

		HashSet<IMyShipConnector> TakenConnectors { get; } = new HashSet<IMyShipConnector>();

		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Set update frequency.
			Runtime.UpdateFrequency = UpdateFrequency.Update100;

			// Set broadcast listeners.
			IGC.RegisterBroadcastListener(Communicator.tagDockRequest);

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

			Echo($"ID: {Me.EntityId}");
			RuntimeStartNs = DateTime.Now.Ticks;
			List<IMyShipConnector> connectors = null;

			// TODO: Cancel dockings that take too long.

			// Send messages.
			int messagesSent = 0;
			{
				foreach(DockingDrone drone in DockingDrones) {
					Quaternion rotation;
					Me.Orientation.GetQuaternion(out rotation);
					var data = Communicator.MakeDockUpdateMessageData(Me.GetPosition(), rotation);
					IGC.SendUnicastMessage(drone.Address, Communicator.tagDockUpdate, data);
				}
			}

			// Recieve messages.
			int messagesRecieved = 0, messagesRecognized = 0, messagesSuccess = 0;
			foreach(MyIGCMessage message in QueuedMessages) {
				messagesRecieved++;
				messagesRecognized++;

				switch(message.Tag) {

					case Communicator.tagDockRequest: {

						// Find closest connector.
						if(connectors == null) connectors = Connectors;
						Vector3D location;
						if(!Communicator.ParseDockRequestMessageData(message.As<string>(), out location)) break;
						IMyShipConnector closest = null;
						double distanceSquared = double.PositiveInfinity;
						foreach(IMyShipConnector connector in connectors) {
							// Skip taken connectors.
							if(TakenConnectors.Contains(connector)) continue;
							// Find closest connector that is available.
							double dist = (connector.GetPosition() - location).LengthSquared();
							if(dist < distanceSquared) {
								distanceSquared = dist;
								closest = connector;
							}
						}

						// Mark connector as used.
						TakenConnectors.Add(closest);
						DockingDrones.Add(new DockingDrone(message.Source, closest));

						// Send message back.
						float length = (Base6Directions.GetIntVector(Base6Directions.Direction.Forward) * closest.Max).AbsMax();
						var data = Communicator.MakeDockAcceptMessageData(length, closest.Orientation.Forward, closest.Position, Base6Directions.GetIntVector(closest.Orientation.Up));
						IGC.SendUnicastMessage(message.Source, Communicator.tagDockAccept, data);
						messagesSuccess++;
						break;

					}

					default:
						messagesRecognized--;
						break;

				}

				if(CurrentRuntimeNs > TargetRuntimeNs) break;

			}

			Echo($"Sent {messagesSent} messages and recieved {messagesSuccess}/{messagesRecognized}/{messagesRecieved} messages.");
			Echo($"Execution time was {CurrentRuntimeNs * 1e-6:N6}ms.");

		}

		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/>or some other means.
		/// </summary>
		public void Save() {

		}

		struct DockingDrone {

			public long Address { get; }
			public IMyShipConnector Connector { get; }

			public DockingDrone(long droneID, IMyShipConnector connector) {
				Address = droneID;
				Connector = connector;
			}

		}

	}

}
