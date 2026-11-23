using UnityEngine;

namespace MultiplayerSFS.Mod {
public class ClientManagerPump : MonoBehaviour
{
	private void Update()
	{
		ClientManager.Update();
		LocalManager.Update();
	}
}
}
