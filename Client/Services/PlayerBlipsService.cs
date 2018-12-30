using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FGMM.SDK.Client.Events;
using FGMM.SDK.Client.RPC;
using FGMM.SDK.Client.Services;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Core.RPC.Events;
using FGMM.Gamemode.TDM.Shared.Models;
using FGMM.Gamemode.TDM.Shared.Events;
using FGMM.SDK.Core.RPC;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using System.Threading;


namespace FGMM.Gamemode.TDM.Client.Services
{
    class PlayerBlipsService : Service
    {
        private const int MAX_PLAYERS = 64;

        private Dictionary<int, int> PlayerTeams;

        public PlayerBlipsService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {

            Rpc.Event(TDMEvents.PlayerAdded).On<int, int>(OnPlayerAddedToTeam);
            TickManager.Attach(GamerTagTick);
            TickManager.Attach(BlipTick);

            PlayerTeams = new Dictionary<int, int>();
        }

        public void Clear()
        {
            PlayerTeams.Clear();
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!API.NetworkIsPlayerActive(i) || API.PlayerId() == i)
                    continue;

                Ped playerPed = new Ped(API.GetPlayerPed(i));
                Blip playerBlip = new Blip(API.GetBlipFromEntity(playerPed.Handle));
                playerBlip?.Delete();
            }
        }

        private void OnPlayerAddedToTeam(IRpcEvent rpc, int player, int team)
        {
            player = API.GetPlayerFromServerId(player);
            PlayerTeams[player] = team;         
        }

        private async Task GamerTagTick()
        {
            if (!PlayerTeams.ContainsKey(API.PlayerId()))
                return;
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!API.NetworkIsPlayerActive(i) || API.PlayerId() == i)
                    continue;

                // Update player Teams
                if (!PlayerTeams.ContainsKey(i))
                    PlayerTeams[i] = await Rpc.Event(TDMEvents.GetPlayerTeam).Request<int>(i);

                // Player is not logged in yet
                if (PlayerTeams[i] == -1)
                    continue;

                int gamerTag = API.CreateMpGamerTag(API.GetPlayerPed(i), API.GetPlayerName(i), false, false, "", 0);
                API.SetMpGamerTagVisibility(gamerTag, 2, true);
                API.SetMpGamerTagAlpha(gamerTag, 2, 255);

                if (PlayerTeams[i] != PlayerTeams[API.PlayerId()])
                {
                    API.SetMpGamerTagColour(gamerTag, 0, 7);
                    API.SetMpGamerTagHealthBarColor(gamerTag, 7);
                }
                else
                {
                    API.SetMpGamerTagColour(gamerTag, 0, 10);
                    API.SetMpGamerTagHealthBarColor(gamerTag, 10);
                }

            }
            await BaseScript.Delay(1000);
        }
           
        private async Task BlipTick()
        {
            if (!PlayerTeams.ContainsKey(API.PlayerId()))
                return;

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!API.NetworkIsPlayerActive(i) || API.PlayerId() == i)
                    continue;

                // Update player Teams
                if (!PlayerTeams.ContainsKey(i))
                {
                    Blip blip = new Blip(API.GetBlipFromEntity(API.GetPlayerPed(i)));
                    blip?.Delete();
                    PlayerTeams[i] = await Rpc.Event(TDMEvents.GetPlayerTeam).Request<int>(i);
                }
                    

                // Player is not logged in yet
                if (PlayerTeams[i] == -1)
                {
                    Blip blip = new Blip(API.GetBlipFromEntity(API.GetPlayerPed(i)));
                    blip?.Delete();
                    continue;
                }
                    

                Ped playerPed = new Ped(API.GetPlayerPed(i));
                Blip playerBlip = new Blip(API.GetBlipFromEntity(playerPed.Handle));
                if(playerBlip == null || !playerBlip.Exists()) // Player has no blip
                {
                    playerBlip = new Blip(API.AddBlipForEntity(playerPed.Handle));
                    playerBlip.Sprite = BlipSprite.Enemy;
                    if (PlayerTeams[i] != PlayerTeams[API.PlayerId()])
                        playerBlip.Color = BlipColor.Red;
                    else
                        playerBlip.Color = BlipColor.Blue;
                }
            }
            await BaseScript.Delay(1000);
        }
    }
}
