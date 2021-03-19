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
		/// Made by Grant Shotwell (https://github.com/SonicBlue22).
		/// </remarks>
		public static void RotateTo(Quaternion current, Quaternion target, ICollection<IMyGyro> gyroscopes) {

			target.Normalize();
			current.Normalize();

			// Axis of the current rotation.
			Vector3D currentX = current * new Vector3(1, 0, 0);
			Vector3D currentY = current * new Vector3(0, 1, 0);
			Vector3D currentZ = current * new Vector3(0, 0, 1);

			// Rotate the whole system so that the 'current' axis are now the world axis.
			var inverse = Quaternion.Inverse(target);
			Vector3D alignedX = inverse * currentX;
			Vector3D alignedY = inverse * currentY;
			Vector3D alignedZ = inverse * currentZ;

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
			double x_xy = Math.Asin(Math.Abs(alignedX.Z) / (1 + Math.Abs(alignedX.Z))) * Math.Sign(alignedX.Z) / Math.PI;
			double x_xz = Math.Asin(Math.Abs(alignedX.Y) / (1 + Math.Abs(alignedX.Y))) * Math.Sign(alignedX.Y) / Math.PI;

			// Goal of gyroscopes: The aligned Y axis needs to be rotated towards the world Y/Z plane and the X/Y plane.
			double y_yz = Math.Asin(Math.Abs(alignedY.X) / (1 + Math.Abs(alignedY.X))) * Math.Sign(alignedY.X) / Math.PI;
			double y_xy = Math.Asin(Math.Abs(alignedY.Z) / (1 + Math.Abs(alignedY.Z))) * Math.Sign(alignedY.Z) / Math.PI;

			// Goal of gyroscopes: The aligned Z axis needs to be rotated towards the world X/Z plane and the Y/Z plane.
			double z_xz = Math.Asin(Math.Abs(alignedZ.Y) / (1 + Math.Abs(alignedZ.Y))) * Math.Sign(alignedZ.Y) / Math.PI;
			double z_yz = Math.Asin(Math.Abs(alignedZ.X) / (1 + Math.Abs(alignedZ.X))) * Math.Sign(alignedZ.X) / Math.PI;

			// Makes ship rotate up/down.
			// Relevant to the Y/Z plane. Rotates around X axis.
			float x = (float)(y_yz + z_yz);

			// Makes ship rotate left/right.
			// Relevant to the X/Z plane. Rotates around Y axis.
			float y = (float)(x_xz + z_xz);

			// Makes ship roll counter/clockwise.
			// Relevant to the X/Y plane. Rotates around Z axis.
			float z = (float)(x_xy + y_xy);

			int axis = -1;
			foreach(IMyGyro gyroscope in gyroscopes) {

				Vector3 rotation = new Vector3(x, y, z) * gyroscope.GetMaximum<float>("Yaw") * 0.30f;

				switch((++axis > 2) ? (axis = 0) : (axis)) {
					case 0:
						rotation *= Vector3.Right;
						break;
					case 1:
						rotation *= Vector3.Up;
						break;
					case 2:
						rotation *= Vector3.Forward;
						break;
				}

				Matrix matrix;
				gyroscope.Orientation.GetMatrix(out matrix);
				rotation = Vector3.Transform(rotation, matrix);

				gyroscope.Yaw = +rotation.Y;
				gyroscope.Pitch = -rotation.X;
				gyroscope.Roll = -rotation.Z;

				gyroscope.GyroOverride = true;
				gyroscope.GyroPower = 1.00f;

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
