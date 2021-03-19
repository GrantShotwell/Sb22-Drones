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

		public static string MakeDockAcceptMessageData(float length, Base6Directions.Direction forwardLocal, Vector3 positionLocal, Vector3I upLocal) {
			string[] elements = new string[] {
				length.ToString(),
				((int)forwardLocal).ToString(),
				positionLocal.X.ToString(), positionLocal.Y.ToString(), positionLocal.Z.ToString(),
				upLocal.X.ToString(), upLocal.Y.ToString(), upLocal.Z.ToString()
			};
			return string.Join(",", elements);
		}

		public static bool ParseDockAcceptMessageData(string data, out float length, out Base6Directions.Direction direction, out Vector3 positionLocal, out Vector3I upLocal) {
			string[] elements = data.Split(',');
			length = 0;
			positionLocal = Vector3D.Zero;
			upLocal = Vector3I.Zero;
			direction = 0;

			if(elements.Length != 8) return false;
			if(!float.TryParse(elements[0], out length)) return false;
			int dir;
			if(!int.TryParse(elements[0], out dir)) return false;
			else direction = (Base6Directions.Direction)dir;
			if(!float.TryParse(elements[1], out positionLocal.X)) return false;
			if(!float.TryParse(elements[2], out positionLocal.Y)) return false;
			if(!float.TryParse(elements[3], out positionLocal.Z)) return false;
			if(!int.TryParse(elements[4], out upLocal.X)) return false;
			if(!int.TryParse(elements[5], out upLocal.Y)) return false;
			if(!int.TryParse(elements[6], out upLocal.Z)) return false;
			return true;

		}


		public const string tagDockUpdate = "sb22.dock_update";

		public static string MakeDockUpdateMessageData(Vector3D gridPosition, Quaternion gridRotation) {
			string[] elements = new string[] {
				gridPosition.X.ToString(), gridPosition.Y.ToString(), gridPosition.Z.ToString(),
				gridRotation.X.ToString(), gridRotation.Y.ToString(), gridRotation.Z.ToString(), gridRotation.W.ToString()
			};
			return string.Join(",", elements);
		}

		public static bool ParseDockUpdateMessageData(string data, out Vector3D gridPosition, out Quaternion gridRotation) {
			string[] elements = data.Split(',');
			gridPosition = Vector3D.Zero;
			gridRotation = Quaternion.Zero;

			if(elements.Length != 7) return false;
			if(!double.TryParse(elements[0], out gridPosition.X)) return false;
			if(!double.TryParse(elements[1], out gridPosition.Y)) return false;
			if(!double.TryParse(elements[2], out gridPosition.Z)) return false;
			if(!float.TryParse(elements[3], out gridRotation.X)) return false;
			if(!float.TryParse(elements[4], out gridRotation.Y)) return false;
			if(!float.TryParse(elements[5], out gridRotation.Z)) return false;
			if(!float.TryParse(elements[6], out gridRotation.W)) return false;
			return true;

		}

	}

}
