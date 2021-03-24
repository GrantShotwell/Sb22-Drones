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
		/// <param name="speed">The rotational speed in rotations per second.</param>
		/// <param name="echo">An output for debugging.</param>
		/// <returns><see langword="true"/> if sitting on the target; <see langword="false"/> if still rotating.</returns>
		/// <remarks>
		/// <para>Will reach the desired rotation within 0.001π radians (0.18­°).</para>
		/// <para>Made by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.</para>
		/// </remarks>
		public static bool RotateTo(Quaternion grid, Quaternion target, IMyShipController control, ICollection<IMyGyro> gyroscopes,
		float speed = 1f, Action<string> echo = null) {

			// Normalize quaternions. Don't deal with zeros.
			if(target == Quaternion.Zero) return true;
			target.Normalize();
			if(grid == Quaternion.Zero) return true;
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


			// Make math easier by aligning target axis with coordinate space.
			Quaternion q = Quaternion.Inverse(target) * grid;
			Vector3 alignedY = q * new Vector3(0f, 1f, 0f);
			Vector3 alignedZ = q * new Vector3(0f, 0f, 1f);

			// Gyroscopes don't use pitch, yaw, and roll. Instead, they take rotation vectors.
			// For each axis to align we need the cross product of current and target
			// with a length of rotation speed (directly proportional to angle).

			// Angle between two vectors:
			// θ = cos⁻¹((a·b)/(|a|·|b|))

			// Gyroscope rotation speed input is in radians per second.
			// Angle is already in radians, so we don't have to do anything there!

			bool sittingY = false;
			float angleY = (float)Math.Acos(alignedY.Y / alignedY.Length());
			Vector3 crossY = new Vector3(-alignedY.Z, 0f, +alignedY.X);
			if(crossY.Normalize() > 0.001f) crossY *= angleY * speed;
			else { crossY = Vector3D.Zero; sittingY = true; }

			bool sittingZ = false;
			float angleZ = (float)Math.Acos(alignedZ.Z / alignedZ.Length());
			Vector3 crossZ = new Vector3(+alignedZ.Y, -alignedZ.X, 0f);
			if(crossZ.Normalize() > 0.001f) crossZ *= angleZ * speed;
			else { crossZ = Vector3D.Zero; sittingZ = true; }

			if(sittingY && sittingZ) return true;

			// Debug output current axis angles.
			if(echo != null) {
				echo($"{angleY * 180f / (float)Math.PI:N1} {(crossY / angleY).ToString("N2")}");
				echo($"{angleZ * 180f / (float)Math.PI:N1} {(crossZ / angleZ).ToString("N2")}");
			}

			// Apply rotation vectors to gyroscopes. Only one rotation vector per gyroscope.
			// Since there are two rotation vectors, switch between them each time.
			bool axis = false;

			foreach(IMyGyro gyroscope in gyroscopes) {

				// Get rotation vector.
				Vector3 rotation = (axis = !axis) ? crossZ : crossY;
				
				// Rotate rotation vector to fit this gyroscope.
				Quaternion orientation;
				gyroscope.Orientation.GetQuaternion(out orientation);
				rotation = Quaternion.Inverse(orientation) * rotation;

				// Apply rotation vector.
				gyroscope.Pitch = -rotation.X;
				gyroscope.Yaw = -rotation.Y;
				gyroscope.Roll = -rotation.Z;

				// Safe to assume caller wants gyroscope override.
				gyroscope.GyroOverride = true;

			}

			return false;

		}

		/// <summary>
		/// Uses <paramref name="thrusters"/> to move the grid along <paramref name="target"/>.
		/// </summary>
		/// <param name="current">The current world position of the grid.</param>
		/// <param name="target">The target world position of the grid.</param>
		/// <param name="thrusters">The <see cref="IMyThrust"/>s to use to move the grid.</param>
		/// <param name="control">A source of grid info to potentially account for when moving.</param>
		/// <param name="speed">The max speed in meters per second to travel.</param>
		/// <param name="velocity">The velocity to be moving at when at the target.</param>
		/// <param name="delay">The time in seconds between function calls. Default value is one game tick.</param>
		/// <param name="echo">An output for debugging.</param>
		/// <returns><see langword="true"/> if sitting on the target with desired velocity; <see langword="false"/> if still moving.</returns>
		/// <remarks>
		/// <para>Will reach the destination within a 1m (2 small grid blocks) diameter.
		/// Will not match target velocity (TODO).</para>
		/// <para>Made by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.</para>
		/// </remarks>
		public static bool MoveTo(Vector3D current, Vector3D target, ICollection<IMyThrust> thrusters, IMyShipController control,
		float speed = 10f, Vector3 velocity = default(Vector3), float delay = 0.01667f, Action<string> echo = null) {

			// Set default value(s).
			if(velocity == default(Vector3)) velocity = Vector3.Zero;

			// Calculate displacement.
			Vector3 S = target - current;
			float s = S.Length();

			if(s < 0.5f && velocity == Vector3.Zero) {
				if(echo != null) echo("Sitting.");
				DisableOverride(thrusters);
				return true;
			}

			// Calculate direction by normalizing displacement.
			Vector3 direction = S;
			direction.Normalize();
			// Get grid's mass.
			float m = control.CalculateShipMass().TotalMass;
			// Get grid's linear velocity (game has inverted it, for some reason).
			Vector3 linear = control.GetShipVelocities().LinearVelocity;
			// Calculate the current velocity towards target (note: misuse of 'ref' makes me sad).
			Vector3 V0 = Vector3.ProjectOnVector(ref linear, ref direction);
			float v0 = V0.Length() * Math.Sign(Vector3.Dot(V0, direction));
			// Calculate current velocity off target (note: the misuse of 'ref' isn't even consistent).
			Vector3 W0 = Vector3.Reject(linear, direction);
			float w0 = W0.Length();
			// Calculate the force needed to correct the 'off-target' velocity.
			Vector3 W0F;
			if(w0 < 0.001f) W0F = Vector3.Zero;
			else W0F = m * -W0 / delay;

			// Sum maximum forwards/backwards force.
			float brakeForce = 0f, accelForce = 0f;
			foreach(IMyThrust thruster in thrusters) {
				float thrust = Vector3.Dot(direction, thruster.WorldMatrix.Forward) * thruster.MaxEffectiveThrust;
				if(thrust > 0) accelForce += thrust;
				else brakeForce += -thrust;
			}

			// Forwards Acceleration
			float a1 = accelForce / m;
			Vector3 A1 = direction * a1;
			// Backwards Acceleration
			float a2 = brakeForce / m;
			Vector3 A2 = direction * a2;
			// Cruise Velocity
			float v1 = speed;
			Vector3 V1 = direction * v1;

			// Calculate distance to brake from current velocity to zero.
			float t = v0 / a2;
			float d = v0 * t + 0.5f * a2 * t * t;
			// Calculate distance traveled between delay.
			float e = v0 * delay;

			bool brake = d > 0f && e > 0f && s <= d + e;
			bool accel = !brake && (v0 < speed);
			Vector3 F;

			if(brake) {
				F = -(m * V0 / t);
			} else if(accel) {
				F = m * (V1 - V0) / (m * (v1 - v0) / accelForce);
			} else {
				F = Vector3.Zero;
			}

			// Debug output current relevent variables.
			if(echo != null) {
				echo($"current speed: {v0:N2}m/s");
				echo($"offset speed: {w0:N2}m/s");
				echo($"total distance: {s:N2}m");
				echo($"brake distance: {d:N2}m ({t:N1}s)");
				if(accel) echo("ACCEL");
				else if(brake) echo("BRAKE");
				else echo("WAIT");
			}

			// Apply thruster overrides.
			foreach(IMyThrust thruster in thrusters) {

				bool overridden = false;
				
				// Apply forward/backward thrust.
				float f = Vector3.Dot(F, thruster.WorldMatrix.Backward);
				if(f > 0f) { thruster.ThrustOverride = f; overridden = true; }

				// Apply 'off target' thrust.
				float w = Vector3.Dot(W0F, thruster.WorldMatrix.Backward);
				if(w > 0f) { thruster.ThrustOverride = w; overridden = true; }

				// Remember to disable override!
				if(!overridden) thruster.ThrustOverride = 0f;
				if(echo != null) echo($"{thruster.CustomName}: {thruster.ThrustOverride * 1e-3:N1}kN");

			}

			return false;

		}

		public static void DisableOverride(ICollection<IMyThrust> thrusters) {
			foreach(IMyThrust thruster in thrusters) thruster.ThrustOverride = 0f;
		}

	}

}
