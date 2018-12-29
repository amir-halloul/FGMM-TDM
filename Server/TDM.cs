using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FGMM.SDK.Server.Diagnostics;
using FGMM.SDK.Server.Gamemodes;
using FGMM.SDK.Server.Models;
using FGMM.Gamemode.TDM.Server.Controllers;
using FGMM.SDK.Server.Events;
using FGMM.SDK.Server.RPC;
using FGMM.SDK.Server.Controllers;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Core.RPC.Events;
using FGMM.Gamemode.TDM.Shared.Events;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace FGMM.Gamemode.TDM.Server
{
    class TDM : Controller, IGamemode
    {
        public IMission Mission { get; set; }
        public int RemaingingTime { get; set; }

        private Mission _Mission { get; set; }
        private GameController GameController { get; set; }

        private bool AwaitingTieBreaker { get; set; }
        private Timer MissionTimer { get; set; }

        public TDM(ILogger logger, IEventManager events, IRpcHandler rpc) : base(logger, events, rpc)
        {
            Events = events;
            Rpc = rpc;
            GameController = new GameController(new Logger("TDM|GameController"), Events, Rpc);
            MissionTimer = new Timer(1000);
            MissionTimer.AutoReset = true;
            MissionTimer.Elapsed += MissionTimer_Elapsed;
        }

        public void Start(string mission)
        {
            Logger.Info($"Loading mission: {mission}");
            AwaitingTieBreaker = false;

            // Load mission
            string Path = $"{API.GetResourcePath(API.GetCurrentResourceName())}/Missions/{mission}";
            Mission = Server.Mission.Load(Path);
            _Mission = Mission as Mission;
            RemaingingTime = Mission.Duration;
            if (_Mission.SelectionData.Teams.Count != 2)
                throw new Exception("This TDM gamemode requires 2 teams.");

            GameController.StartMission(_Mission);

            // Start game timer          
            MissionTimer.Start();       

            Logger.Debug($"Loaded: {_Mission.Name}");
        }

        private void MissionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RemaingingTime--;
            // When mission time runs out
            if (RemaingingTime <= 0)
            {
                if (GameController.IsGameTied())
                {
                    Logger.Debug("Mission time ended but the game is tied. Awaiting tie breaker...");
                    AwaitingTieBreaker = true;
                }
                else
                    Events.Raise(ServerEvents.EndMission);

                // Stop timer
                MissionTimer.Stop();
            }
        }

        public void Stop()
        {
            Logger.Info("Stop TDM gamemode...");

            // Stop timer if not stopped yet
            if (MissionTimer != null)
                MissionTimer.Stop();
        }

        public bool HandleTeamJoinRequest(Player player, int team)
        {
            // If the player can join chosen team
            if (GameController.AddPlayerToTeam(player, team))
            {
                Rpc.Event(TDMEvents.UpdateTimer).Trigger(player, RemaingingTime);
                GameController.SpawnPlayer(player);
                return true;
            }
            return false;
        }

        public void HandleDeath(Player player, Player killer)
        {
            GameController.ProcessDeathEvent(player, killer, AwaitingTieBreaker);
        }

        public void HandleDisconnect(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
