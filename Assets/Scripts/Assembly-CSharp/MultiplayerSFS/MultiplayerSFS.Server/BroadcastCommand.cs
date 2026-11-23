using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using Lidgren.Network;
using MultiplayerSFS.ServerCommon;
using UnityEngine;

namespace MultiplayerSFS.Server {
public class BroadcastCommand : Command
{
	public override string Description => "Broadcasts a message to players. This command requires admin privileges.\nbroadcast <message> [player] [type] [color]:\n  <message>: The message content to broadcast\n  [player]: Optional, default is \"all\" (all players), or specify a player's username\n  [type]: Optional, default is \"message\" (chat message), or \"toast\" (floating window)\n  [color]: Optional, default is server message color, or specify in #RRGGBB or #RRGGBBAA format\nExample: broadcast Welcome to the server PLAYER1 message #66ccff or broadcast Hello all message #FF000080 (semi-transparent red)";

	public override string Run(string[] args, NetConnection sender)
	{
		if (!CheckAdmin(sender))
		{
			return "You do not have the required admin privileges to use this command.";
		}
		if (args.Length == 0)
		{
			return "Not enough arguments provided. Please specify a message.";
		}
		List<string> list = new List<string>(args);
		string text = list[0];
		string text2 = "all";
		string text3 = "message";
		string text4 = null;
		int num = 1;
		while (num < list.Count)
		{
			string text5 = list[num];
			if (text5.ToLower() == "message" || text5.ToLower() == "toast")
			{
				text3 = text5.ToLower();
				num++;
				continue;
			}
			if (text5.ToLower() == "all")
			{
				text2 = "all";
				num++;
				continue;
			}
			if (!text5.StartsWith("#") || (text5.Length != 7 && text5.Length != 9))
			{
				break;
			}
			text4 = text5;
			num++;
		}
		if (num < list.Count)
		{
			List<string> range = list.GetRange(num, list.Count - num);
			for (int i = 0; i < range.Count; i++)
			{
				string text6 = range[i];
				if (text6.ToLower() == "message" || text6.ToLower() == "toast")
				{
					text3 = text6.ToLower();
					range.RemoveAt(i);
					i--;
				}
				else if (text6.ToLower() == "all")
				{
					text2 = "all";
					range.RemoveAt(i);
					i--;
				}
				else if (text6.StartsWith("#") && (text6.Length == 7 || text6.Length == 9))
				{
					text4 = text6;
					range.RemoveAt(i);
					i--;
				}
			}
			if (range.Count > 0)
			{
				string text7 = range[0];
				foreach (ConnectedPlayer value in Server.connectedPlayers.Values)
				{
					if (value.username.Equals(text7, StringComparison.OrdinalIgnoreCase))
					{
						text2 = text7;
						range.RemoveAt(0);
						break;
					}
				}
				if (range.Count > 0)
				{
					text = text + " " + string.Join(" ", range);
				}
			}
		}
		System.Drawing.Color color = System.Drawing.Color.White;
		if (!string.IsNullOrEmpty(text4) && text4.StartsWith("#") && (text4.Length == 7 || text4.Length == 9))
		{
			try
			{
				int red = Convert.ToInt32(text4.Substring(1, 2), 16);
				int green = Convert.ToInt32(text4.Substring(3, 2), 16);
				int blue = Convert.ToInt32(text4.Substring(5, 2), 16);
				int alpha = 255;
				if (text4.Length == 9)
				{
					alpha = Convert.ToInt32(text4.Substring(7, 2), 16);
				}
				color = System.Drawing.Color.FromArgb(alpha, red, green, blue);
			}
			catch
			{
			}
		}
		if (text2.Equals("all", StringComparison.OrdinalIgnoreCase))
		{
			if (text3.Equals("toast", StringComparison.OrdinalIgnoreCase))
			{
				Server.SendPacketToAll(new Packet_ShowToastMessage
				{
					Message = text
				}, null, (NetDeliveryMethod)67);
			}
			else
			{
				Server.SendPacketToAll(new Packet_SendChatMessage
				{
					SenderId = -1,
					Message = text,
					Color = color
				}, null, (NetDeliveryMethod)67);
			}
			return "Broadcasted message to all players: " + text;
		}
		foreach (KeyValuePair<IPEndPoint, ConnectedPlayer> connectedPlayer in Server.connectedPlayers)
		{
			if (connectedPlayer.Value.username.Equals(text2, StringComparison.OrdinalIgnoreCase))
			{
				if (text3.Equals("toast", StringComparison.OrdinalIgnoreCase))
				{
					Server.SendPacketToPlayer(((NetPeer)Server.server).GetConnection(connectedPlayer.Key), new Packet_ShowToastMessage
					{
						Message = text
					}, (NetDeliveryMethod)67);
				}
				else
				{
					Server.SendPacketToPlayer(((NetPeer)Server.server).GetConnection(connectedPlayer.Key), new Packet_SendChatMessage
					{
						SenderId = -1,
						Message = text,
						Color = color
					}, (NetDeliveryMethod)67);
				}
				return "Broadcasted message to player " + text2 + ": " + text;
			}
		}
		return "Player " + text2 + " not found.";
	}
}
}
