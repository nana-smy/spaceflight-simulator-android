using UnityEngine;
using UnityEngine.Serialization;
// 预制体引用由 Unity Inspector 序列化赋值，非代码赋值
#pragma warning disable 0649

namespace SFS.UI.ModGUI
{
	internal class ModGUIPrefabsLoader : MonoBehaviour
	{
		internal static ModGUIPrefabsLoader main;

		public GameObject windowPrefab;

		[FormerlySerializedAs("layoutContainer")]
		public GameObject containerPrefab;

		public GameObject boxPrefab;

		public GameObject separatorPrefab;

		[FormerlySerializedAs("blueButtonPrefab")]
		public GameObject buttonPrefab;

		public GameObject buttonWithLabelPrefab;

		public GameObject inputFieldPrefab;

		public GameObject inputWithLabelPrefab;

		public GameObject togglePrefab;

		public GameObject toggleWithLabelPrefab;

		public GameObject labelPrefab;

		public GameObject sliderPrefab;

		public GameObject webImage;

		private ModGUIPrefabsLoader()
		{
			main = this;
		}
	}
}
