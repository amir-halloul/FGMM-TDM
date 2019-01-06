using System.Threading.Tasks;
using FGMM.SDK.Client.Events;
using FGMM.SDK.Client.RPC;
using FGMM.SDK.Client.Services;
using FGMM.SDK.Core.Diagnostics;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;

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
            foreach(Player player in new PlayerList())
            {
                if (player == Game.Player)
                    continue;        

                int playerTeam = API.GetPlayerTeam(player.Handle);               
                
                int gamerTag = API.CreateMpGamerTag(player.Character.Handle, player.Name, false, false, "", 0);

                RaycastResult raycastResult = World.Raycast(Game.PlayerPed.Position, player.Character.Position, IntersectOptions.Map);

                if (raycastResult.DitHit || playerTeam == -1)
                {
                    API.SetMpGamerTagVisibility(gamerTag, 0, false);
                    API.SetMpGamerTagVisibility(gamerTag, 2, false);
                }
                else
                {
                    API.SetMpGamerTagVisibility(gamerTag, 0, true);
                    API.SetMpGamerTagVisibility(gamerTag, 2, true);
                }

                API.SetMpGamerTagAlpha(gamerTag, 2, 255);

                if (playerTeam != API.GetPlayerTeam(Game.Player.Handle))
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
            await BaseScript.Delay(500);
        }
           
        private async Task BlipTick()
        {
            foreach(Player player in new PlayerList())
            {
                if (player == Game.Player)
                    continue;
               
                int playerTeam = API.GetPlayerTeam(player.Handle);

                Blip blip = new Blip(API.GetBlipFromEntity(player.Character.Handle));
                blip?.Delete();

                if(playerTeam != -1)
                {
                    blip = new Blip(API.AddBlipForEntity(player.Character.Handle));
                    blip.Sprite = BlipSprite.Enemy;

                    if (API.GetPlayerTeam(Game.Player.Handle) == playerTeam)
                        blip.Color = BlipColor.Blue;
                    else
                        blip.Color = BlipColor.Red;
                }
            }
            await BaseScript.Delay(1000);
        }
    }
}
