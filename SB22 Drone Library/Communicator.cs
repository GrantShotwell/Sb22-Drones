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
using System.Text.RegularExpressions;
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

namespace Sb22.Drones {

	public static class Communicator {

		public static int Version => 1;


		public const string tagDockRequest = "sb22.dock_request";

		public static string MakeDockRequestMessageData(Vector3D connectorPosition) {
			string[] elements = new string[] {
				connectorPosition.X.ToString(), connectorPosition.Y.ToString(), connectorPosition.Z.ToString()
			};
			return string.Join(",", elements);
		}

		public static bool ParseDockRequestMessageData(string data, out Vector3D connectorPosition) {
			string[] elements = data.Split(',');
			connectorPosition = Vector3D.Zero;

			if(elements.Length != 3) return false;
			if(!double.TryParse(elements[0], out connectorPosition.X)) return false;
			if(!double.TryParse(elements[1], out connectorPosition.Y)) return false;
			if(!double.TryParse(elements[2], out connectorPosition.Z)) return false;
			return true;

		}


		public const string tagDockAccept = "sb22.dock_accept";

		public static string MakeDockAcceptMessageData(float length) {
			string[] elements = new string[] {
				length.ToString()
			};
			return string.Join(",", elements);
		}

		public static bool ParseDockAcceptMessageData(string data, out float length) {
			string[] elements = data.Split(',');
			length = 0;

			if(elements.Length != 1) return false;
			if(!float.TryParse(elements[0], out length)) return false;
			return true;

		}


		public const string tagDockUpdate = "sb22.dock_update";

		public static string MakeDockUpdateMessageData(Vector3D connectorPosition, Quaternion connectorRotation, Vector3 connectorVelocity) {
			string[] elements = new string[] {
				connectorPosition.X.ToString(), connectorPosition.Y.ToString(), connectorPosition.Z.ToString(),
				connectorRotation.X.ToString(), connectorRotation.Y.ToString(), connectorRotation.Z.ToString(), connectorRotation.W.ToString(),
				connectorVelocity.X.ToString(), connectorVelocity.Y.ToString(), connectorVelocity.Z.ToString()
			};
			return string.Join(",", elements);
		}

		public static bool ParseDockUpdateMessageData(string data, out Vector3D connectorPosition, out Quaternion connectorRotation, out Vector3 connectorVelocity) {
			string[] elements = data.Split(',');
			connectorPosition = Vector3D.Zero;
			connectorRotation = Quaternion.Zero;
			connectorVelocity = Vector3.Zero;

			if(elements.Length != 7) return false;
			if(!double.TryParse(elements[0], out connectorPosition.X)) return false;
			if(!double.TryParse(elements[1], out connectorPosition.Y)) return false;
			if(!double.TryParse(elements[2], out connectorPosition.Z)) return false;
			if(!float.TryParse(elements[3], out connectorRotation.X)) return false;
			if(!float.TryParse(elements[4], out connectorRotation.Y)) return false;
			if(!float.TryParse(elements[5], out connectorRotation.Z)) return false;
			if(!float.TryParse(elements[6], out connectorRotation.W)) return false;
			if(!float.TryParse(elements[7], out connectorVelocity.X)) return false;
			if(!float.TryParse(elements[8], out connectorVelocity.Y)) return false;
			if(!float.TryParse(elements[9], out connectorVelocity.Z)) return false;
			return true;

		}

	}

}
