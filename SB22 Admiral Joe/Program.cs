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

	partial class Program : MyGridProgram {

		string ControllerName { get; } = "Clang Controller";
		string FrontGroup { get; } = "Front Clang";
		string BackGroup { get; } = "Back Clang";
		string LeftGroup { get; } = "Left Clang";
		string RightGroup { get; } = "Right Clang";
		string UpGroup { get; } = "Up Clang";
		string DownGroup { get; } = "Down Clang";


		public Program() {
			Runtime.UpdateFrequency = UpdateFrequency.Update1;
		}

		public void Save() {

		}

		public void Main(string argument, UpdateType updateSource) {

			var front = new List<IMyTerminalBlock>();
			var back = new List<IMyTerminalBlock>();
			var right = new List<IMyTerminalBlock>();
			var left = new List<IMyTerminalBlock>();
			var up = new List<IMyTerminalBlock>();
			var down = new List<IMyTerminalBlock>();

			GridTerminalSystem.GetBlockGroupWithName(FrontGroup)?.GetBlocks(front);
			GridTerminalSystem.GetBlockGroupWithName(BackGroup)?.GetBlocks(back);
			GridTerminalSystem.GetBlockGroupWithName(RightGroup)?.GetBlocks(right);
			GridTerminalSystem.GetBlockGroupWithName(LeftGroup)?.GetBlocks(left);
			GridTerminalSystem.GetBlockGroupWithName(UpGroup)?.GetBlocks(up);
			GridTerminalSystem.GetBlockGroupWithName(DownGroup)?.GetBlocks(down);

			var controller = GridTerminalSystem.GetBlockWithName(ControllerName) as IMyShipController;
			var input = GetInput(controller);

			if(input.LengthSquared() != 0) {
				Echo(input.ToString());
			}

			ApplyInput(
				input,
				front: front,
				back: back,
				right: right,
				left: left,
				up: up,
				down: down
			);

		}

		
		Vector3 GetInput(IMyShipController controller) {
			return controller?.MoveIndicator ?? Vector3.Zero;
		}

		void ApplyInput(
			Vector3 input,
			IEnumerable<IMyTerminalBlock> front,
			IEnumerable<IMyTerminalBlock> back,
			IEnumerable<IMyTerminalBlock> right,
			IEnumerable<IMyTerminalBlock> left,
			IEnumerable<IMyTerminalBlock> up,
			IEnumerable<IMyTerminalBlock> down
		) {

			// Forward/backward axis.
			if(input.Z == 0) {
				DeactivateBlocks(front, back);
			} else if(input.Z > 0) {
				ApplyAxis(front, back);
			} else {
				ApplyAxis(back, front);
			}

			// Right/left axis.
			if(input.X == 0) {
				DeactivateBlocks(left, right);
			} else if(input.X > 0) {
				ApplyAxis(right, left);
			} else {
				ApplyAxis(left, right);
			}

			// Up/down axis.
			if(input.Y == 0) {
				DeactivateBlocks(up, down);
			} else if(input.Y > 0) {
				ApplyAxis(up, down);
			} else {
				ApplyAxis(down, up);
			}

		}

		void ApplyAxis(
			IEnumerable<IMyTerminalBlock> activate,
			IEnumerable<IMyTerminalBlock> deactivate
		) {
			ActivateBlocks(activate);
			DeactivateBlocks(deactivate);
		}

		void ActivateBlocks(
			params IEnumerable<IMyTerminalBlock>[] blocks
		) {
			foreach(var enumerable in blocks) {
				foreach(var block in enumerable) {
					var merge = block as IMyShipMergeBlock;
					if(merge != null)
						merge.Enabled = true;
				}
			}
		}

		void DeactivateBlocks(
			params IEnumerable<IMyTerminalBlock>[] blocks
		) {
			foreach(var enumerable in blocks) {
				foreach(var block in enumerable) {
					var merge = block as IMyShipMergeBlock;
					if(merge != null)
						merge.Enabled = false;
				}
			}
		}

	}

}
