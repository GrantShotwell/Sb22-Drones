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

		long RuntimeStartNs { get; set; }
		long CurrentRuntimeNs => DateTime.Now.Ticks - RuntimeStartNs;
		long TargetRuntimeNs { get; } = (long)1e6 / 10;

		ulong MessagesOut { get; set; } = 0;
		ulong MessagesIn { get; set; } = 0;

		IMyBroadcastListener ListenerDockAccept { get; }

		IEnumerable<MyIGCMessage> QueuedMessages {
			get {
				while(IGC.UnicastListener.HasPendingMessage)
					yield return IGC.UnicastListener.AcceptMessage();
				while(ListenerDockAccept.HasPendingMessage)
					yield return ListenerDockAccept.AcceptMessage();
			}
		}

		private readonly List<IMyShipConnector> connectors = new List<IMyShipConnector>();
		List<IMyShipConnector> Connectors {
			get {
				GridTerminalSystem.GetBlocksOfType(connectors,
					connector => connector.CubeGrid == Me.CubeGrid);
				return connectors;
			}
		}

		Queue<DockingDrone> QueuedDockingDrones { get; } = new Queue<DockingDrone>();
		ICollection<DockingDrone> DockingDrones { get; } = new LinkedList<DockingDrone>();

		HashSet<IMyShipConnector> TakenConnectors { get; } = new HashSet<IMyShipConnector>();

		/// <summary>
		/// The one and only constructor.
		/// </summary>
		public Program() {

			// Get the main text surface of the programmable block.
			Console = new ConsoleHelper(Me.GetSurface(0));

			// Load from storage.
			if(!string.IsNullOrWhiteSpace(Storage)) {

				try {

					string storage = Storage;

					var links = new LinkedList<char>();
					for(int i = 0; i < storage.Length; i++) {

						if(storage[i] == '=') {

							char[] array = links.ToArray();
							string section = new string(array);
							links.Clear();

							switch(section) {

								case "docking": {
									var element = new LinkedList<char>();
									var elements = new LinkedList<string>();
									while(++i < storage.Length) {
										if(storage[i] == ',') element.AddLast(storage[i]);
										else if(storage[i] == '}') break;
										else element.AddLast(storage[i]);
									}
									LinkedListNode<string> elem = elements.First;
									int count = elements.Count;
									DockingDrone[] drones = new DockingDrone[count / 3];
									for(int j = 0; j < count; j += 3) {
										long address = long.Parse(elem.Value);
										elem = elem.Next;
										long connector = long.Parse(elem.Value);
										elem = elem.Next;
										uint ticks = uint.Parse(elem.Value);
										elem = elem.Next;
										drones[j / 3] = new DockingDrone(address, connector, ticks, GridTerminalSystem);
									}
									DockingDrones = new LinkedList<DockingDrone>(drones);
									break;
								}

								case "dockingQ": {
									var element = new LinkedList<char>();
									var elements = new LinkedList<string>();
									while(++i < storage.Length) {
										if(storage[i] == ',') element.AddLast(storage[i]);
										else if(storage[i] == '}') break;
										else element.AddLast(storage[i]);
									}
									LinkedListNode<string> elem = elements.First;
									int count = elements.Count;
									DockingDrone[] drones = new DockingDrone[count / 3];
									for(int j = 0; j < count; j += 3) {
										long address = long.Parse(elem.Value);
										elem = elem.Next;
										long connector = long.Parse(elem.Value);
										elem = elem.Next;
										uint ticks = uint.Parse(elem.Value);
										elem = elem.Next;
										drones[j / 3] = new DockingDrone(address, connector, ticks, GridTerminalSystem);
									}
									QueuedDockingDrones = new Queue<DockingDrone>(drones);
									break;
								}

								case "takenConnectors": {
									var element = new LinkedList<char>();
									var elements = new LinkedList<string>();
									while(++i < storage.Length) {
										if(storage[i] == ',') element.AddLast(storage[i]);
										else if(storage[i] == '}') break;
										else element.AddLast(storage[i]);
									}
									LinkedListNode<string> elem = elements.First;
									int count = elements.Count;
									IMyShipConnector[] connectors = new IMyShipConnector[count];
									for(int j = 0; j < count; j += 3) {
										long id = long.Parse(elem.Value);
										elem = elem.Next;
										connectors[j] = GridTerminalSystem.GetBlockWithId(id) as IMyShipConnector;
									}
									TakenConnectors = new HashSet<IMyShipConnector>(connectors);
									break;
								}

								default: {
									Console.WriteLine($"Unknown storage section '{section}'.");
									break;
								}

							}

						} else {

							links.AddLast(storage[i]);

						}

					}

					Console.WriteLine("Loaded from storage.");

				} catch(Exception e) {

					Console.WriteLine("Error loading from storage.");
					Console.WriteLine(e.GetType().Name);

				}

			}

			// Set update frequency.
			Runtime.UpdateFrequency = UpdateFrequency.Update1;

			// Set broadcast listeners.
			ListenerDockAccept = IGC.RegisterBroadcastListener(Communicator.tagDockRequest);

			// Finish.
			Console.WriteLine("Program constructed.");
			Console.Apply();

		}


		/// <summary>
		/// Called when the program needs to save its state.
		/// Use this method to save your state to <see cref="MyGridProgram.Storage"/> or some other means.
		/// </summary>
		public void Save() {

			StringBuilder storage = new StringBuilder();
			bool comma;

			// Docking Drones
			storage.Append("docking=");
			storage.Append(DockingDrones.Count);
			storage.Append("{");
			comma = false;
			foreach(DockingDrone drone in DockingDrones) {
				if(comma) storage.Append(',');
				AppendStorage(storage, drone);
				comma = true;
			}
			storage.Append("}");

			// Docking Queue
			storage.Append("dockingQ=");
			storage.Append(QueuedDockingDrones.Count);
			storage.Append("{");
			comma = false;
			foreach(DockingDrone drone in QueuedDockingDrones) {
				if(comma) storage.Append(',');
				AppendStorage(storage, drone);
				comma = true;
			}
			storage.Append("}");

			// Taken Connectors
			storage.Append("takenConnectors=");
			storage.Append(TakenConnectors.Count);
			storage.Append("{");
			comma = false;
			foreach(IMyShipConnector connector in TakenConnectors) {
				if(comma) storage.Append(',');
				AppendStorage(storage, connector);
				comma = true;
			}
			storage.Append("{");

			Storage = storage.ToString();

		}

		private void AppendStorage(StringBuilder storage, DockingDrone drone) {
			storage.Append(drone.Address.ToString());
			storage.Append(',');
			storage.Append(drone.Connector.EntityId.ToString());
			storage.Append(',');
			storage.Append(drone.Ticks.ToString());
		}

		private void AppendStorage(StringBuilder storage, IMyShipConnector connector) {
			storage.Append(connector.EntityId.ToString());
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
			Echo($"Last execution took {Runtime.LastRunTimeMs:N6}ms.");
			RuntimeStartNs = DateTime.Now.Ticks;
			List<IMyShipConnector> connectors = null;

			// Cancel dockings that take too long.
			{
				foreach(DockingDrone drone in DockingDrones.ToArray()) {
					drone.Tick();
					if(drone.Ticks > 7200) {
						DockingDrones.Remove(drone);
						TakenConnectors.Remove(drone.Connector);
						if(QueuedDockingDrones.Count > 0) {
							DockingDrones.Add(QueuedDockingDrones.Dequeue());
						}
					}
				}
			}

			// Send messages.
			{
				foreach(DockingDrone drone in DockingDrones.ToArray()) {
					Quaternion rotation = Quaternion.CreateFromRotationMatrix(drone.Connector.WorldMatrix);
					Vector3 velocity = Vector3.Zero;
					var data = Communicator.MakeDockUpdateMessageData(drone.Connector.GetPosition(), rotation, velocity);
					if(IGC.SendUnicastMessage(drone.Address, Communicator.tagDockUpdate, data)) MessagesOut++;
					else {
						// TEMP: Remove drone if disconnected.
						DockingDrones.Remove(drone);
						TakenConnectors.Remove(drone.Connector);
					}
				}
			}

			// Recieve messages.
			foreach(MyIGCMessage message in QueuedMessages) {

				MessagesIn++;
				switch(message.Tag) {

					case Communicator.tagDockRequest: {

						// Find closest connector.
						if(connectors == null) connectors = Connectors;
						Vector3D location;
						if(!Communicator.ParseDockRequestMessageData(message.Data as string, out location)) break;

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

						if(closest == null) {
							break;
						}

						// Mark connector as used.
						TakenConnectors.Add(closest);
						DockingDrones.Add(new DockingDrone(message.Source, closest));

						// Send message back.
						float length = (Base6Directions.GetIntVector(Base6Directions.Direction.Forward) * closest.Max).AbsMax();
						var data = Communicator.MakeDockAcceptMessageData(length);
						if(IGC.SendUnicastMessage(message.Source, Communicator.tagDockAccept, data)) {
							MessagesOut++;
							break;
						} else {
							break;
						}

					}

				}

				if(CurrentRuntimeNs > TargetRuntimeNs) break;

			}

			Console.Apply();
			Echo($"Total Messages: {MessagesIn:N0} in / {MessagesOut:N0} out.");

		}

		struct DockingDrone {

			public long Address { get; }
			public IMyShipConnector Connector { get; set; }
			public uint Ticks { get; set; }

			public DockingDrone(long droneID, IMyShipConnector connector) {
				Address = droneID;
				Connector = connector;
				Ticks = 0;
			}

			public DockingDrone(long droneID, long connectorID, uint ticks, IMyGridTerminalSystem terminal) {
				Address = droneID;
				Connector = terminal.GetBlockWithId(connectorID) as IMyShipConnector;
				Ticks = ticks;
			}

			public void Tick() => Ticks++;

		}

	}

}
