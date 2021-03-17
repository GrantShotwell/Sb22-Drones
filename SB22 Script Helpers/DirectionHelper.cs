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

namespace Sb22.ScriptHelpers {

	public static class DirectionHelper {

		/// <summary>
		/// Finds the direction on the same <see cref="Axis"/> as <paramref name="direction"/> but opposite.
		/// </summary>
		/// <param name="direction">The reference <see cref="Base6Directions.Direction"/>.</param>
		/// <returns>Returns the inverted <see cref="Base6Directions.Direction"/>.</returns>
		public static Base6Directions.Direction Invert(Base6Directions.Direction direction) {
			return (int)direction % 2 == 0 ? direction - 1 : direction + 1;
		}

		/// <summary>
		/// Inverts the given <see cref="Base6Directions.Direction"/>.
		/// </summary>
		/// <param name="direction">The <see cref="Base6Directions.Direction"/> to invert.</param>
		/// <returns>Returns the inverted <see cref="Base6Directions.Direction"/>.</returns>
		public static Base6Directions.Direction Invert(ref Base6Directions.Direction direction) {
			return (int)direction % 2 == 0 ? --direction : ++direction;
		}

		/// <summary>
		/// If <paramref name="b"/> is a direction local to direction <paramref name="a"/>, finds the world direction of <paramref name="b"/>.
		/// </summary>
		/// <param name="a">The <see cref="Base6Directions.Direction"/> <paramref name="b"/> is local to.</param>
		/// <param name="b">The local <see cref="Base6Directions.Direction"/> local to <see cref="a"/>.</param>
		public static Base6Directions.Direction Apply(Base6Directions.Direction a, Base6Directions.Direction b) {

			switch(a) {
				case Base6Directions.Direction.Forward:
					return b;
				case Base6Directions.Direction.Backward:
					switch(b) {
						case Base6Directions.Direction.Forward:
							return Base6Directions.Direction.Backward;
						case Base6Directions.Direction.Backward:
							return Base6Directions.Direction.Forward;
						case Base6Directions.Direction.Left:
							return Base6Directions.Direction.Right;
						case Base6Directions.Direction.Right:
							return Base6Directions.Direction.Left;
						case Base6Directions.Direction.Up:
						case Base6Directions.Direction.Down:
							return b;
						default: return 0;
					}
				case Base6Directions.Direction.Left:
					switch(b) {
						case Base6Directions.Direction.Forward:
							return Base6Directions.Direction.Left;
						case Base6Directions.Direction.Backward:
							return Base6Directions.Direction.Right;
						case Base6Directions.Direction.Left:
							return Base6Directions.Direction.Backward;
						case Base6Directions.Direction.Right:
							return Base6Directions.Direction.Forward;
						case Base6Directions.Direction.Up:
						case Base6Directions.Direction.Down:
							return b;
						default: return 0;
					}
				case Base6Directions.Direction.Right:
					switch(b) {
						case Base6Directions.Direction.Forward:
							return Base6Directions.Direction.Right;
						case Base6Directions.Direction.Backward:
							return Base6Directions.Direction.Left;
						case Base6Directions.Direction.Left:
							return Base6Directions.Direction.Forward;
						case Base6Directions.Direction.Right:
							return Base6Directions.Direction.Backward;
						case Base6Directions.Direction.Up:
						case Base6Directions.Direction.Down:
							return b;
						default: return 0;
					}
				case Base6Directions.Direction.Up:
					switch(b) {
						case Base6Directions.Direction.Forward:
							return Base6Directions.Direction.Up;
						case Base6Directions.Direction.Backward:
							return Base6Directions.Direction.Down;
						case Base6Directions.Direction.Left:
						case Base6Directions.Direction.Right:
							return b;
						case Base6Directions.Direction.Up:
							return Base6Directions.Direction.Backward;
						case Base6Directions.Direction.Down:
							return Base6Directions.Direction.Forward;
						default: return 0;
					}
				case Base6Directions.Direction.Down:
					switch(b) {
						case Base6Directions.Direction.Forward:
							return Base6Directions.Direction.Down;
						case Base6Directions.Direction.Backward:
							return Base6Directions.Direction.Up;
						case Base6Directions.Direction.Left:
						case Base6Directions.Direction.Right:
							return b;
						case Base6Directions.Direction.Up:
							return Base6Directions.Direction.Forward;
						case Base6Directions.Direction.Down:
							return Base6Directions.Direction.Backward;
						default: return 0;
					}
				default: return 0;
			}

		}

	}

}
