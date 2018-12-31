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
    /*
     * At the moment this just shows you a message with your own stats
     * TODO: Make it an actual leaderboard
     * */
    class LeaderboardService : Service
    {
        private Scaleform LeaderboardSC;
        private bool ShowLeaderboard;

        public LeaderboardService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {
            LeaderboardSC = new Scaleform("mp_big_message_freemode");
            Rpc.Event(ServerEvents.MissionEnded).On(OnMissionEnded);
            Rpc.Event(ServerEvents.MissionStarted).On(OnMissionStarted);
            
            TickManager.Attach(LeaderboardTick);
        }

        private void OnMissionStarted(IRpcEvent obj)
        {
            ShowLeaderboard = false;
        }

        private async Task UpdateLeaderboard(Result result)
        {
            while (!LeaderboardSC.IsLoaded)
                await BaseScript.Delay(100);
            LeaderboardSC.CallFunction("SHOW_MISSION_PASSED_MESSAGE", result.Won? "Winner" : "Loser", "", 100, true, 0, true);
        }

        private async Task LeaderboardTick()
        {
            if (ShowLeaderboard)
                LeaderboardSC.Render2D();
        }

        private async void OnMissionEnded(IRpcEvent rpc)
        {
            Result result = await Rpc.Event(TDMEvents.GetResult).Request<Result>();
            UpdateLeaderboard(result);
            ShowLeaderboard = true;
        }

    }
}
