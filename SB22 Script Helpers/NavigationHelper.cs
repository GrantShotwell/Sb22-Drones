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
		/// <param name="gyroscopes">The <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <param name="speed">The rotational speed in rotations per second.</param>
		/// <param name="echo">An output for debugging.</param>
		/// 
		/// <returns><see langword="true"/> if sitting on the target; <see langword="false"/> if still rotating.</returns>
		/// <remarks>
		/// <para>Will reach the desired rotation within 0.001π radians (0.18°).</para>
		/// <para>Made by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.</para>
		/// </remarks>
		public static bool RotateTo(
			Quaternion grid,
			Quaternion target,
			ICollection<IMyGyro> gyroscopes,
			float speed = 1f,
			Action<string> echo = null
		) {

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

			/* Gyroscopes don't use pitch, yaw, and roll. Instead, they take rotation vectors.
			 * For each axis to align we need the cross product of current and target
			 * with a length of rotation speed (directly proportional to angle).
			 * 
			 * Angle between two vectors:
			 * θ = cos⁻¹((a·b)/(|a|·|b|))
			 * 
			 * Gyroscope rotation speed input is in radians per second.
			 * Angle is already in radians, so we don't have to do anything there!
			 */

			bool sittingY = false;
			float angleY = (float)Math.Acos(alignedY.Y / alignedY.Length());
			Vector3 crossY = new Vector3(-alignedY.Z, 0f, +alignedY.X);
			if(crossY.Normalize() > 0.001f) {
				crossY *= angleY * speed;
			} else {
				crossY = Vector3D.Zero;
				sittingY = true;
			}

			bool sittingZ = false;
			float angleZ = (float)Math.Acos(alignedZ.Z / alignedZ.Length());
			Vector3 crossZ = new Vector3(+alignedZ.Y, -alignedZ.X, 0f);
			if(crossZ.Normalize() > 0.001f) {
				crossZ *= angleZ * speed;
			} else {
				crossZ = Vector3.Zero;
				sittingZ = true;
			}

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

				// Get rotation vector (change the axis every loop).
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
		/// Uses <paramref name="thrusters"/> to move the grid towards <paramref name="target"/> until it lands with the target velocity.
		/// </summary>
		/// <param name="current">The current world position of the grid.</param>
		/// <param name="target">The target world position of the grid.</param>
		/// <param name="thrusters">The <see cref="IMyThrust"/>s to use to move the grid.</param>
		/// <param name="control">A source of grid info to potentially account for when moving.</param>
		/// <param name="speed">The max speed in meters per second to travel.</param>
		/// <param name="velocity">The current velocity of <paramref name="target"/>. Magnitude should be sufficiently less than <paramref name="speed"/>.</param>
		/// <param name="delay">The time in seconds between method calls. Default value is one game tick.</param>
		/// <param name="echo">An output for debugging.</param>
		/// <returns><see langword="true"/> if sitting on the target with desired velocity; <see langword="false"/> if still adjusting and/or moving.</returns>
		/// <remarks>
		/// <para>
		/// Will reach the destination within a 0.5m (1 small grid length) radius.
		/// Uses <see href="https://en.wikipedia.org/wiki/Vector_projection">vector projection and rejection</see> to determine the forces to apply.
		/// Therefore, if the grid is facing the target without any rejected velocity then only forwards/backwards thrusters will be used.
		/// Otherwise, not having enough thrusters in every direction is insufficient.
		/// </para>
		/// <para>
		/// When a thruster is not needed, it is disabled. When it is, it is enabled.
		/// Because of this, make sure <paramref name="thrusters"/> was created by checking if each thruster is <see cref="IMyFunctionalBlock.Enabled"/>.
		/// That way, the ship pilot can enable/disable backup hydrogen thrusters, for example, and not have them re-enabled by this method.
		/// </para>
		/// <para>
		/// Made by <see href="https://github.com/SonicBlue22">Grant Shotwell</see>.
		/// </para>
		/// </remarks>
		public static bool MoveTo(
			Vector3D current,
			Vector3D target,
			ICollection<IMyThrust> thrusters,
			IMyShipController control,
			float speed = float.PositiveInfinity,
			Vector3 velocity = default(Vector3),
			float delay = 1f / 60f,
			Action<string> echo = null
		) {

			// Local constant
			float sitRadius = 0.5f;

			// Calculate displacement.
			Vector3 S = target - current + velocity * delay;
			float s = S.Length();
			// Calculate direction by normalizing displacement.
			Vector3 direction = S;
			direction.Normalize();
			// Get 'zero'.
			Vector3 VZero = velocity;
			float vZero = VZero.Length();
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
			// Default to zero when below a low arbitrary theshold.
			Vector3 W = w0 < 0.001f ? Vector3.Zero : m * -W0 / delay;

			// Do we need to sit?
			if(velocity == Vector3.Zero && s < sitRadius) {
				echo?.Invoke("Sitting.");
				RemoveOverride(thrusters);
				return true;
			}

			// Sum maximum forwards/backwards force.
			float brakeForce = 0f, accelForce = 0f;
			foreach(IMyThrust thruster in thrusters) {
				if(!thruster.Enabled) continue;

				/* 
				 * Magnitude of dot product will be positive when the vectors are similar.
				 * Magnitude will be negative when the vectors are otherwise opposite.
				 */

				float thrust = Vector3.Dot(direction, thruster.WorldMatrix.Backward) * thruster.MaxEffectiveThrust;
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
			Vector3 V1 = direction * v1 + vZero;

			// Calculate distance to brake from current velocity to zero.
			float t = (v0 - vZero) / a2;
			float d = v0 * t + 0.5f * a2 * t * t;
			// Calculate distance that will be traveled between delay.
			float e = v0 * delay;

			// Decide if we need to brake, accelerate, or wait.
			bool brake = d > 0f && e > 0f && s <= d + e;
			bool accel = !brake && (v0 < speed);
			Vector3 F;

			// Get the thrust force.
			if(brake) {
				F = m * (VZero - V0) / t;
			} else if(accel) {
				F = m * (V1 - V0) / (m * (v1 - v0) / accelForce);
			} else {
				F = Vector3.Zero;
			}

			// Debug output current relevent variables.
			if(echo != null) {
				echo($"project speed: {v0:N2}m/s");
				echo(V0.ToString("N2"));
				echo($"reject speed: {w0:N2}m/s");
				echo(W0.ToString("N2"));
				echo($"total distance: {s:N2}m");
				echo($"brake distance: {d:N2}m ({t:N1}s)");
				if(accel) echo("ACCEL");
				else if(brake) echo("BRAKE");
				else echo("WAIT");
			}

			// Apply thruster overrides.
			foreach(IMyThrust thruster in thrusters) {
				if(!thruster.Enabled) continue;
				bool overridden = false;

				/* 
				 * Magnitude of dot product is proportional magnutide of both vectors multiplied.
				 * In this case, one of the vectors is a length of one.
				 */

				// Apply project thrust.
				float f = Vector3.Dot(F, thruster.WorldMatrix.Backward);
				if(f > 0f) {
					thruster.ThrustOverride = f;
					overridden = true;
				}

				// Apply reject thrust.
				float w = Vector3.Dot(W, thruster.WorldMatrix.Backward);
				if(w > 0f) {
					thruster.ThrustOverride = w;
					overridden = true;
				}

				// Remember to disable override!
				if(!overridden) {
					thruster.ThrustOverride = 0f;
					thruster.Enabled = false;
				}

				// Debug output thruster names and their overrides.
				echo?.Invoke($"{thruster.CustomName}: {thruster.ThrustOverride * 1e-3:N1}kN");

			}

			return false;

		}

		/// <summary>
		/// Iterates through all thrusters in <paramref name="thrusters"/>
		/// and disables their <see cref="IMyThrust.ThrustOverride"/>.
		/// </summary>
		/// <param name="thrusters"><see cref="IMyThrust"/> objects to disable override.</param>
		public static void RemoveOverride(IEnumerable<IMyThrust> thrusters) {
			foreach(IMyThrust thruster in thrusters) {
				thruster.Enabled = true;
				thruster.ThrustOverride = 0f;
			}
		}

	}

}
