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
		/// <param name="current">The target world vector.</param>
		/// <param name="target">The current world vector.</param>
		/// <param name="gyroscopes">The collection of <see cref="IMyGyro"/>s to use to rotate the grid.</param>
		/// <remarks>
		/// Made by Grant Shotwell (https://github.com/SonicBlue22).
		/// </remarks>
		public static void RotateTo(Quaternion current, Quaternion target, ICollection<IMyGyro> gyroscopes, out string debug) {

			#region my failures
			StringBuilder debugBuilder = new StringBuilder();
			Quaternion difference = current * Quaternion.Inverse(target);

			target.Normalize();
			current.Normalize();

			// Axis of the current rotation.
			Vector3D targetX = target * new Vector3(1, 0, 0);
			Vector3D targetY = target * new Vector3(0, 1, 0);
			Vector3D targetZ = target * new Vector3(0, 0, 1);

			// Rotate the whole system so that the 'current' axis are now the world axis.
			var inverse = Quaternion.Inverse(current);
			Vector3D alignedX = inverse * targetX;
			Vector3D alignedY = inverse * targetY;
			Vector3D alignedZ = inverse * targetZ;

			debugBuilder.Append($"Aligned X: {alignedX.ToString("N2")}\n");
			debugBuilder.Append($"Aligned Y: {alignedY.ToString("N2")}\n");
			debugBuilder.Append($"Aligned Z: {alignedZ.ToString("N2")}\n");

			// Each world axis has two perpendicular planes that go along it.
			// Planes can be defined by their normal vector and any point on the plane.
			// Since we know the planes are at the origin, we only need the normal vector.
			//Vector3 xyPlane = new Vector3(0, 0, 1);
			//Vector3 yzPlane = new Vector3(1, 0, 0);
			//Vector3 xzPlane = new Vector3(0, 1, 0);
			// However, since the plane normals have components of just zero and one,
			// it is easier and more efficient to simplify the equasions for each angle.

			// Angle (a) between a plane (normal=n) at the origin and a vector (v) is
			// a = arcsin( |n∙v| / (|n|∙|v|) )
			// Since every plane's normal vector has a single non-zero component, one,
			// then we can simplify the equasion to be relevant to the single component (w).
			// a = arcsin( |w| / (1+|w|) )

			// Goal of gyroscopes: The aligned X axis needs to be rotated towards the world X/Y plane and the X/Z plane.
			double x_xy = Math.Asin(Math.Abs(alignedX.Z) / (1 + Math.Abs(alignedX.Z))) * Math.Sign(alignedX.Z);
			double x_xz = Math.Asin(Math.Abs(alignedX.Y) / (1 + Math.Abs(alignedX.Y))) * Math.Sign(alignedX.Y);

			// Goal of gyroscopes: The aligned Y axis needs to be rotated towards the world Y/Z plane and the X/Y plane.
			double y_yz = Math.Asin(Math.Abs(alignedY.X) / (1 + Math.Abs(alignedY.X))) * Math.Sign(alignedY.X);
			double y_xy = Math.Asin(Math.Abs(alignedY.Z) / (1 + Math.Abs(alignedY.Z))) * Math.Sign(alignedY.Z);

			// Goal of gyroscopes: The aligned Z axis needs to be rotated towards the world X/Z plane and the Y/Z plane.
			double z_xz = Math.Asin(Math.Abs(alignedZ.Y) / (1 + Math.Abs(alignedZ.Y))) * Math.Sign(alignedZ.Y);
			double z_yz = Math.Asin(Math.Abs(alignedZ.X) / (1 + Math.Abs(alignedZ.X))) * Math.Sign(alignedZ.X);

			// Makes ship rotate up/down.
			// Relevant to the Y/Z plane. Rotates around X axis.
			float x
				= (float)(y_yz * 1 + z_yz * 1) * 1;
				//= (float)(x_xy * 0 + x_xz * 0) * 1;

			// Makes ship rotate left/right.
			// Relevant to the X/Z plane. Rotates around Y axis.
			float y
				= (float)(x_xz * 1 + z_xz * 1) * 1;
				//= (float)(y_xy * 0 + y_yz * 0) * 1;

			// Makes ship roll counter/clockwise.
			// Relevant to the X/Y plane. Rotates around Z axis.
			float z
				= (float)(x_xy * 1 + y_xy * 1) * 1;
			//= (float)(z_xz * 1 + z_yz * 1) * 1;

			debugBuilder.Append($"X: {y_yz:N2} + {z_yz:N2} = {x:N2}\n");
			debugBuilder.Append($"Y: {x_xz:N2} + {z_xz:N2} = {y:N2}\n");
			debugBuilder.Append($"Z: {x_xy:N2} + {y_xy:N2} = {z:N2}\n");
			debug = debugBuilder.ToString();

			//x = 1;
			//y = 2;
			//z = 3;

			foreach(IMyGyro gyroscope in gyroscopes) {

				MyBlockOrientation orientation = gyroscope.Orientation;

				float _x, _y, _z;

				switch(DirectionHelper.Invert(orientation.Left)) {
					case Base6Directions.Direction.Forward:
						_x = +y; break;
					case Base6Directions.Direction.Backward:
						_x = -y; break;
					case Base6Directions.Direction.Right:
						_x = +x; break;
					case Base6Directions.Direction.Left:
						_x = -x; break;
					case Base6Directions.Direction.Up:
						_x = +z; break;
					case Base6Directions.Direction.Down:
						_x = -z; break;
					default: _x = 0f; break;
				}

				switch(orientation.Up) {
					case Base6Directions.Direction.Forward:
						_y = +y; break;
					case Base6Directions.Direction.Backward:
						_y = -y; break;
					case Base6Directions.Direction.Right:
						_y = +x; break;
					case Base6Directions.Direction.Left:
						_y = -x; break;
					case Base6Directions.Direction.Up:
						_y = +z; break;
					case Base6Directions.Direction.Down:
						_y = -z; break;
					default: _y = 0f; break;
				}

				switch(orientation.Forward) {
					case Base6Directions.Direction.Forward:
						_z = +y; break;
					case Base6Directions.Direction.Backward:
						_z = -y; break;
					case Base6Directions.Direction.Right:
						_z = +x; break;
					case Base6Directions.Direction.Left:
						_z = -x; break;
					case Base6Directions.Direction.Up:
						_z = +z; break;
					case Base6Directions.Direction.Down:
						_z = -z; break;
					default: _z = 0f; break;
				}

				gyroscope.SetValue("Yaw", +y);
				gyroscope.SetValue("Pitch", -x);
				gyroscope.SetValue("Roll", -z);

				gyroscope.GyroOverride = true;
				gyroscope.GyroPower = 1.00f;

			}
			#endregion

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
