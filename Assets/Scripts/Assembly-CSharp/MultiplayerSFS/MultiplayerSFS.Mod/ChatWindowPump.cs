using UnityEngine;

namespace MultiplayerSFS.Mod {
public class ChatWindowPump : MonoBehaviour
{
	private void Update()
	{
		ChatWindow.Update();
	}
}
}
