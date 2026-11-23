using System;
using UnityEngine;

namespace SFS.Parts.Modules
{
	[Serializable]
	public class Orientation
	{
		public float x = 1f;

		public float y = 1f;

		public float z;

		public Orientation(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public bool InversedAxis()
		{
			return Mathf.Abs(z % 180f) > 0.001f;
		}

		public static Vector2 operator *(Vector2 a, Orientation orientation)
		{
			return Quaternion.Euler(0f, 0f, orientation.z) * new Vector2(a.x * orientation.x, a.y * orientation.y);
		}

		public static Orientation operator +(Orientation a, Orientation change)
		{
			// 单轴镜像会反转旋转方向：S(-1,1) * R(z) = R(-z) * S(-1,1)，
			// 这样任意倾斜角（如 45°）的零件镜像后仍能保持正确形状。
			bool flipX = change.x < 0f;
			bool flipY = change.y < 0f;
			if (flipX != flipY)
			{
				return new Orientation(flipX ? (0f - a.x) : a.x, flipY ? (0f - a.y) : a.y, change.z - a.z);
			}
			return new Orientation(flipX ? (0f - a.x) : a.x, flipY ? (0f - a.y) : a.y, change.z + a.z);
		}

		public Orientation GetCopy()
		{
			return new Orientation(x, y, z);
		}
	}
}
