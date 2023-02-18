using System;
using UnityEngine;

namespace ToonRP.Runtime
{
	[Serializable]
	public struct ToonRampSettings
	{
		[Range(-1.0f, 1.0f)]
		public float Threshold;
		[Range(0.0f, 2.0f)]
		public float Smoothness;
		public Color ShadowColor;
	}
}