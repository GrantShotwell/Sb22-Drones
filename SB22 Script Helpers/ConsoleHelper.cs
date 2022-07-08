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
using Sb22.Library;

namespace Sb22.ScriptHelpers {

	public class ConsoleHelper {

		public IMyTextSurface TextSurface { get; }
		public int MaxLines { get; }
		private RollingList<string> Lines { get; }


		public ConsoleHelper(IMyTextSurface textSurface, float fontSize = 0.95f, int maxLines = 11) {
			TextSurface = textSurface;
			textSurface.FontSize = fontSize;
			textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
			Lines = new RollingList<string>(MaxLines = maxLines);
		}


		public void WriteLine(object value) {
			Lines.Insert(value.ToString() + '\n');
		}

		public void WriteLine(params object[] values) {
			foreach(object value in values)
				WriteLine(value);
		}

		public void Apply() {
			StringBuilder builder = new StringBuilder(MaxLines);
			foreach(string value in Lines) {
				builder.Append(value);
			}
			TextSurface.WriteText(builder.ToString(), append: false);
		}

	}

}
