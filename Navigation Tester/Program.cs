﻿using Sandbox.Game.EntityComponents;
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

		public Program() {
			Runtime.UpdateFrequency = UpdateFrequency.Update1;
		}

		public void Main() {

			List<IMyGyro> gyroscopes = new List<IMyGyro>();
			GridTerminalSystem.GetBlocksOfType(gyroscopes);

			List<IMyRemoteControl> controls = new List<IMyRemoteControl>();
			GridTerminalSystem.GetBlocksOfType(controls);
			IMyRemoteControl control = controls[0];

			Quaternion current = Quaternion.CreateFromRotationMatrix(control.WorldMatrix);
			Quaternion target = Quaternion.CreateFromForwardUp(Vector3.Forward, Vector3.Up);

			string debug;
			NavigationHelper.RotateTo(current, target, gyroscopes, out debug);
			Echo(debug);

		}

	}
}