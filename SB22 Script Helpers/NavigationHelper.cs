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
		/// <param name="grid">The current rotation.</param>
		/// <param name="target">The target rotation.</param>
		/// <param name="control">A source of grid info to potentially account for when rotating.</param>
		/// <param name="gyroscopes">The <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <param name="speed">The rotational speed, in radians per second, when the axis is a full 180° (π radians) away. Lowered as the angle decreases.</param>
		/// <param name="echo">An output for debugging.</param>
		/// <remarks>
		/// Made by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.
		/// </remarks>
		public static void RotateTo(Quaternion grid, Quaternion target, IMyShipController control, ICollection<IMyGyro> gyroscopes, float speed = 1f, Action<string> echo = null) {

			// Normalize quaternions. Don't deal with zeros.
			if(target == Quaternion.Zero) return;
			target.Normalize();
			if(grid == Quaternion.Zero) return;
			grid.Normalize();

			// Debug output 'not enough gyroscopes' warning.
			if(echo != null && gyroscopes.Count < 2) {
				echo("WARNING: At least two gyroscopes are required to properly rotate.");
			}
			
			// Debug output current quaternion angles.
			if(echo != null) {
				echo(grid.ToString("N2"));
				echo(target.ToString("N2"));
			}

			// The Y axis is reasonable.
			// For some reason, Z axis is backwards.
			Vector3 unitY = new Vector3(0f, +1f, 0f);
			Vector3 unitZ = new Vector3(0f, 0f, -1f);

			// Make math easier by aligning target axis with coordinate space.
			Quaternion inverse = Quaternion.Inverse(target);
			Vector3 alignedY = inverse * (grid * unitY);
			Vector3 alignedZ = inverse * (grid * unitZ);

			// Gyroscopes don't use pitch, yaw, and roll. Instead, they take rotation vectors.
			// For each axis to align we need the cross product of current and target
			// with a length of rotation speed (directly proportional to angle).

			// Angle between two vectors:
			// θ = cos⁻¹((a·b)/(|a|·|b|))

			// Gyroscope rotation speed is in radians per second.
			// Since 'speed' is in radians per second, we need to divide angle by 1 radian unit (π).
			// After that, we can multiply the angle by 'speed' to get the final rotational speed.

			float angleY = (float)Math.Acos(alignedY.Y / alignedY.Length()) / (float)Math.PI;
			Vector3 crossY = new Vector3(-alignedY.Z, 0f, +alignedY.X);
			if(crossY.Normalize() > 0.001f) crossY *= angleY * speed;
			else crossY = Vector3D.Zero;

			float angleZ = (float)Math.Acos(alignedZ.Z / alignedZ.Length()) / (float)Math.PI;
			Vector3 crossZ = new Vector3(+alignedZ.Y, -alignedZ.X, 0f);
			if(crossZ.Normalize() > 0.001f) crossZ *= angleZ * speed;
			else crossZ = Vector3D.Zero;

			// Debug output current axis angles.
			if(echo != null) {
				echo($"{angleY * 180f / (float)Math.PI:N1} {(crossY / angleY).ToString("N2")}");
				echo($"{angleZ * 180f / (float)Math.PI:N1} {(crossZ / angleZ).ToString("N2")}");
			}

			// Apply rotation vectors to gyroscopes. Only one rotation vector per gyroscope.
			// Since there are two rotation vectors, switch between them each time.
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

				gyroscope.GyroOverride = true;

			}

		}

		/// <summary>
		/// Uses <paramref name="thrusters"/> to move the grid along <paramref name="target"/>.
		/// </summary>
		/// <param name="current">The current world position of the grid.</param>
		/// <param name="target">The target world position of the grid.</param>
		/// <param name="thrusters">The <see cref="IMyThrust"/>s to use to move the grid.</param>
		/// <param name="control">A source of grid info to potentially account for when moving.</param>
		/// <param name="echo">An output for debugging.</param>
		public static void MoveTo(Vector3D current, Vector3D target, ICollection<IMyThrust> thrusters, IMyShipController control, float time = 1f, Action<string> echo = null) {

			// Calculate ideal force (no thrust limits).
			float mass = control.CalculateShipMass().TotalMass;
			Vector3 linear = control.GetShipVelocities().LinearVelocity;
			Vector3 distance = current - target;
			Vector3 velocity = distance / time - linear;
			Vector3 acceleration = velocity / time;
			Vector3 force = mass * acceleration;

			// TODO: Account for maximum thrust possible.

			if(echo != null) {
				echo(velocity.ToString("N2"));
			}

			foreach(IMyThrust thruster in thrusters) {

				float dot = Vector3.Dot(thruster.WorldMatrix.Forward, force);
				thruster.ThrustOverride = dot;

				if(echo != null) {
					echo($"{thruster.CustomName}: {dot:N0}");
				}

			}

		}

	}

}
