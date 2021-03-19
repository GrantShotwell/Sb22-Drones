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
		/// Uses <paramref name="gyroscopes"/> to rotate the grid to the target <see cref="Quaternion"/>, <paramref name="target"/>, given the current <see cref="Quaternion"/>, <paramref name="current"/>.
		/// </summary>
		/// <param name="current">The target <see cref="Quaternion"/>.</param>
		/// <param name="target">The current <see cref="Quaternion"/>.</param>
		/// <param name="gyroscopes">The collection of <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <remarks>
		/// Original by <see href="https://github.com/Whiplash141">Josh 'Whiplash141'</see>, re-written by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.
		/// </remarks>
		public static void RotateTo(Quaternion current, Quaternion target, ICollection<IMyGyro> gyroscopes) {

			if(Quaternion.IsZero(current)) return;
			current.Normalize();
			if(Quaternion.IsZero(target)) return;
			target.Normalize();

			Quaternion inverse = Quaternion.Inverse(current);
			Vector3 targetRt = inverse * target.Right;
			Vector3 targetUp = inverse * target.Up;
			Vector3 targetFw = inverse * target.Forward;

			Matrix matrix = Matrix.Zero;
			matrix.Forward = targetFw;
			matrix.Left = -targetRt;
			matrix.Up = targetUp;

			Vector3 axis = new Vector3(matrix.M23 - matrix.M32, matrix.M31 - matrix.M13, matrix.M12 - matrix.M21);
			double trace = matrix.M11 + matrix.M22 + matrix.M33;
			float angle = (float)Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1)) / (float)Math.PI;

			float yaw, pitch, roll;
			if(Vector3.IsZero(axis)) {
				angle = targetFw.Z < 0f ? 0f : (float)Math.PI;
				yaw = angle;
				pitch = 0f;
				roll = 0f;
			} else {
				axis.Normalize();
				yaw = -axis.Y * angle;
				pitch = -axis.X * angle;
				roll = -axis.Z * angle;
			}

			Vector3 rotation = inverse * new Vector3(pitch, yaw, roll);
			foreach(IMyGyro gyroscope in gyroscopes) {

				Vector3 rot = Vector3.TransformNormal(rotation, Matrix.Transpose(gyroscope.WorldMatrix));

				gyroscope.Pitch = rot.X * gyroscope.GetMaximum<float>("Pitch");
				gyroscope.Yaw = rot.Y * gyroscope.GetMaximum<float>("Yaw");
				gyroscope.Roll = rot.Z * gyroscope.GetMaximum<float>("Roll");
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
