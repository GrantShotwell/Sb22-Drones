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
		/// Uses <paramref name="gyroscopes"/> to rotate the grid to the target <see cref="Quaternion"/>, <paramref name="target"/>, given the current <see cref="Quaternion"/>, <paramref name="grid"/>.
		/// </summary>
		/// <param name="grid">The target <see cref="Quaternion"/>.</param>
		/// <param name="target">The current <see cref="Quaternion"/>.</param>
		/// <param name="gyroscopes">The collection of <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <remarks>
		/// Made by Grant Shotwell (https://github.com/SonicBlue22).
		/// </remarks>
		public static void RotateTo(Quaternion offset, Quaternion grid, Quaternion target, IMyShipController control, ICollection<IMyGyro> gyroscopes, Action<string> echo = null) {

			if(target == Quaternion.Zero) return;
			target.Normalize();
			if(grid == Quaternion.Zero) return;
			grid.Normalize();

			target *= offset;
			
			if(echo != null) {
				echo(grid.ToString("N2"));
				echo(target.ToString("N2"));
			}

			Vector3D unitY = new Vector3D(0.0, 1.0, 0.0);
			Vector3D unitZ = new Vector3D(0.0, 0.0, -1.0);

			Vector3D gridY = grid * unitY;
			Vector3D gridZ = grid * unitZ;

			Quaternion inverse = Quaternion.Inverse(target);
			Vector3D alignedY = inverse * gridY;
			Vector3D alignedZ = inverse * gridZ;

			// Angle between two vectors:
			// θ = arcos((a·b)/(|a|·|b|))

			double angleY = Math.Acos(alignedY.Y / alignedY.Length()) / Math.PI;
			Vector3D crossY = new Vector3D(-alignedY.Z, 0.0, +alignedY.X);
			double y = crossY.Normalize();
			if(Math.Abs(y) < 0.001) crossY = Vector3D.Zero;
			else crossY *= angleY;

			double angleZ = Math.Acos(alignedZ.Z / alignedZ.Length()) / Math.PI;
			Vector3D crossZ = new Vector3D(+alignedZ.Y, -alignedZ.X, 0.0);
			double z = crossZ.Normalize();
			if(Math.Abs(z) < 0.001) crossZ = Vector3D.Zero;
			else crossZ *= angleZ;

			if(echo != null) {
				echo($"{angleY * 180.0 / Math.PI:N1} {(crossY / angleY).ToString("N2")}");
				echo($"{angleZ * 180.0 / Math.PI:N1} {(crossZ / angleZ).ToString("N2")}");
			}

			bool axis = false;
			foreach(IMyGyro gyroscope in gyroscopes) {

				if(axis = !axis) {

					Vector3 rotation = -crossZ;

					Quaternion orientation;
					gyroscope.Orientation.GetQuaternion(out orientation);
					rotation = Quaternion.Inverse(orientation) * rotation;

					gyroscope.Pitch = rotation.X;
					gyroscope.Yaw = -rotation.Y;
					gyroscope.Roll = rotation.Z;


				} else {

					Vector3 rotation = crossY;

					Quaternion orientation;
					gyroscope.Orientation.GetQuaternion(out orientation);
					rotation = Quaternion.Inverse(orientation) * rotation;

					gyroscope.Pitch = rotation.X;
					gyroscope.Yaw = rotation.Y;
					gyroscope.Roll = rotation.Z;

				}

				gyroscope.Pitch *= gyroscope.GetMaximum<float>("Pitch");
				gyroscope.Yaw *= gyroscope.GetMaximum<float>("Yaw");
				gyroscope.Roll *= gyroscope.GetMaximum<float>("Roll");
				gyroscope.GyroOverride = true;

			}

		}

		/// <summary>
		/// Uses <paramref name="thrusters"/> to move the grid along <paramref name="vector"/>.
		/// </summary>
		/// <param name="vector">The local direction and magnitude to move the grid.</param>
		/// <param name="thrusters">The collection of <see cref="IMyGyro"/>s to use to move the grid.</param>
		/// <param name="percentage">The percentage of power for each <see cref="IMyThrust"/> to use.</param>
		public static void MoveToLocal(Vector3D vector, ICollection<IMyThrust> thrusters, float percentage = 1.00f) {

			foreach(IMyThrust thruster in thrusters) {

			}

		}

	}

}
