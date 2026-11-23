using System;
using UnityEngine;

namespace SFS.UI.ModGUI
{
	public class Toggle : GUIElement
	{
		public ToggleButton toggleButton;

		public override void Init(GameObject self, Transform parent)
		{
			gameObject = self;
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			rectTransform = gameObject.Rect();
			toggleButton = gameObject.GetComponent<ToggleButton>();
		}
	}
}
