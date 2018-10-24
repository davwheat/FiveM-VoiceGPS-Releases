using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace VoiceGPS_FiveM.Server
{
	public class ServerScript : BaseScript
	{
		public ServerScript()
		{
			API.RegisterCommand("vgps", new Action<int, List<object>, string>((int source, List<object> arguments, string raw) =>
			{
				PlayerList playerList = new PlayerList();
				Player player = playerList[source];

				TriggerClientEvent(player, "vgps:toggleVGPS", new object[0]);
			}), false);

			API.RegisterCommand("vgpsvol", new Action<int, List<object>, string>((int source, List<object> arguments, string raw) =>
			{
				PlayerList playerList = new PlayerList();
				Player player = playerList[source];

				TriggerClientEvent(player, "vgps:adjustVolume", new object[]
				{
					arguments
				});
			}), false);
		}
	}
}
