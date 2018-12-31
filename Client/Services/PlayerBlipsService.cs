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

        public PlayerBlipsService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {
            TickManager.Attach(GamerTagTick);
            TickManager.Attach(BlipTick);
        }

        public void Clear()
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                Ped playerPed = new Ped(API.GetPlayerPed(i));
                Blip playerBlip = new Blip(API.GetBlipFromEntity(playerPed.Handle));
                playerBlip?.Delete();
            }
        }

        private async Task GamerTagTick()
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!API.NetworkIsPlayerActive(i) || API.PlayerId() == i)
                    continue;

                if (API.GetPlayerTeam(i) == -1)
                    continue;  

                int gamerTag = API.CreateMpGamerTag(API.GetPlayerPed(i), API.GetPlayerName(i), false, false, "", 0);
                API.SetMpGamerTagVisibility(gamerTag, 2, true);
                API.SetMpGamerTagAlpha(gamerTag, 2, 255);

                if (API.GetPlayerTeam(i) != API.GetPlayerTeam(Game.Player.Handle))
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
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!API.NetworkIsPlayerActive(i) || API.PlayerId() == i)
                    continue;
                if(API.GetPlayerTeam(i) == -1)
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
                    if (API.GetPlayerTeam(i) != API.GetPlayerTeam(Game.Player.Handle))
                        playerBlip.Color = BlipColor.Red;
                    else
                        playerBlip.Color = BlipColor.Blue;
                }
            }
            await BaseScript.Delay(1000);
        }
    }
}
