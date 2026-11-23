using System;

namespace MultiplayerSFS.Server {
public class ServerConfigVariable : Attribute
{
	public readonly string[] Comment;

	public ServerConfigVariable(params string[] comment)
	{
		Comment = comment;
	}
}
}
