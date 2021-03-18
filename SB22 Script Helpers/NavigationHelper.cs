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

	/// <summary>
	/// A general class for helping scripts navigate.
	/// </summary>
	public static class NavigationHelper {

		/// <summary>
		/// Uses <paramref name="gyroscopes"/> to rotate the grid to the target direction, <paramref name="target"/>, given the current direction, <paramref name="current"/>.
		/// </summary>
		/// <param name="current">The target local vector.</param>
		/// <param name="target">The current local vector.</param>
		/// <param name="gyroscopes">The collection of <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <param name="percentage">The percentage of power for each <see cref="IMyGyro"/> to use.</param>
		/// <remarks>
		/// Math taken from https://forum.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461.
		/// </remarks>
		public static void RotateTo(Vector3 current, Vector3 target, ICollection<IMyGyro> gyroscopes, float percentage = 0.90f) {

			// Don't compute the same angle twice.
			Dictionary<Base6Directions.Direction, float> angles = new Dictionary<Base6Directions.Direction, float>(6);
			Vector3 cross = Vector3.Cross(current, target);
			cross.Normalize();

			foreach(IMyGyro gyroscope in gyroscopes) {

				Base6Directions.Direction orientation = gyroscope.Orientation.Up;
				float angle;

				// Check if we already made this angle.
				if(angles.ContainsKey(orientation)) {

					// Get saved angle.
					angle = angles[orientation];

				} else {

					// The gyroscope controls are lies. Make a rotation vector.
					float y = cross.Length();
					float x = 1 - cross.LengthSquared();
					angle = (float)Math.Atan2(y, Math.Sqrt(x < 0 ? 0 : x));
					angles.Add(orientation, angle / (float)Math.PI);

				}

				// Control rotation speed proportional to angle.
				float power = gyroscope.GetMaximum<float>("Yaw");
				float control = power * angle * percentage;
				control = Math.Min(power, control);
				control = Math.Max(0.01f, control);

				// Apply rotation.
				gyroscope.Pitch = cross.X * control;
				gyroscope.Yaw = -cross.Y * control;
				gyroscope.Roll = -cross.Z * control;
				gyroscope.GyroPower = 1.00f;
				gyroscope.GyroOverride = true;

			}

		}

		/// <summary>
		/// Uses <paramref name="thrusters"/> to move the grid along <paramref name="vector"/>.
		/// </summary>
		/// <param name="vector">The direction and magnitude to move the grid.</param>
		/// <param name="thrusters">The collection of <see cref="IMyGyro"/>s to use to move the grid.</param>
		/// <param name="percentage">The percentage of power for each <see cref="IMyThrust"/> to use.</param>
		public static void MoveToLocal(Vector3D vector, ICollection<IMyThrust> thrusters, float percentage = 1.00f) {



		}

	}

}
