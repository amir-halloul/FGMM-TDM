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
    class LeaderboardService : Service
    {
        private Scaleform LeaderboardSC;

        public LeaderboardService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {
            LeaderboardSC = new Scaleform("mp_celebration");
            Logger.Debug($"Scaleform valid: {LeaderboardSC.IsValid}");         

            TickManager.Attach(LeaderboardTick);
        }

        private async Task LeaderboardTick()
        {
            LeaderboardSC.CallFunction("CREATE_STAT_WALL", "EARLYDEATH", "HUD_COLOUR_BLACK", "100");
            LeaderboardSC.CallFunction("SHOW_STAT_WALL", "EARLYDEATH");
            LeaderboardSC.Render2D();   
        }
    }
}
