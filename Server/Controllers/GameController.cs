using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FGMM.SDK.Server.Controllers;
using FGMM.SDK.Core.Diagnostics;
using FGMM.SDK.Server.Events;
using FGMM.SDK.Server.RPC;
using FGMM.SDK.Gamemodes.Models;
using FGMM.Gamemode.TDM.Shared.Models;
using FGMM.Gamemode.TDM.Shared.Events;
using FGMM.SDK.Core.RPC.Events;
using CitizenFX.Core;

namespace FGMM.Gamemode.TDM.Server.Controllers
{
    class GameController : Controller
    {
        private TeamList TeamList { get; set; }
        private Mission Mission { get; set; }

        public GameController(ILogger logger, IEventManager events, IRpcHandler rpc) : base(logger, events, rpc)
        {               
            Rpc.Event(TDMEvents.RequestRespawnData).On(OnRespawnDataRequested);
            Rpc.Event(TDMEvents.GetPlayerTeam).On<int>(OnPlayerTeamRequested);
        }

        public void StartMission(Mission mission)
        {
            TeamList = new TeamList();
            Mission = mission;

            foreach (SDK.Core.Models.Team team in mission.Teams)
            {
                Team t = new Team();
                t.Name = team.Name;
                t.Score = 0;
                TeamList.Teams.Add(t);
            }
        }

        private void OnPlayerTeamRequested(IRpcEvent rpc, int playerID)
        {
            Player player = new PlayerList()[playerID];
            if (player == null)
                rpc.Reply(-1);
            else
            {
                Team team = TeamList.GetPlayerTeam(player);
                rpc.Reply(TeamList.Teams.IndexOf(team));
            }
        }

        private void OnRespawnDataRequested(IRpcEvent rpc)
        {
            Player player = new PlayerList()[rpc.Client.Handle];
            Team team = TeamList.GetPlayerTeam(player);
            // TODO: keep it dry
            if(team == null)
            {
                Logger.Error($"Player {rpc.Client.Name} ({rpc.Client.Handle}) tried to spawn in an invalid team.");
                return;
            }
            var missionTeam = GetMissionTeamFromGamemodeTeam(team);
            SpawnData spawnData = new SpawnData()
            {
                Position = missionTeam.GetRandomSpawnPoint(),
                Skin = missionTeam.GetRandomSkin(),
                Loadout = missionTeam.Loadout
            };
            rpc.Reply(spawnData);
        }

        public void SpawnPlayer(Player player)
        {
            Rpc.Event(TDMEvents.UpdateScore).Trigger(player, TeamList.Teams[0].Score, TeamList.Teams[1].Score);

            Team team = TeamList.GetPlayerTeam(player);
            if (team == null)
            {
                Logger.Error($"Player {player.Name} ({player.Handle}) tried to spawn in an invalid team.");
                return;
            }
            var missionTeam = GetMissionTeamFromGamemodeTeam(team);
            SpawnData spawnData = new SpawnData()
            {
                Position = missionTeam.GetRandomSpawnPoint(),
                Skin = missionTeam.GetRandomSkin(),
                Loadout = missionTeam.Loadout
            };

            Rpc.Event(TDMEvents.Spawn).Trigger(player, spawnData);
        }

        public bool AddPlayerToTeam(Player player, int team)
        {
            if (!IsValidTeamId(team))
                return false;
            if (!TeamList.CanPlayerJoinTeam(TeamList.Teams[team]))
                return false;
            TeamList.Teams[team].Players.Add(player);
            Rpc.Event(TDMEvents.PlayerAdded).Trigger(int.Parse(player.Handle), team);
            return true;
        }

        public void ProcessDeathEvent(Player player, Player killer, bool tieBreaker = false)
        {
            Logger.Debug($"ProcessDeathEvent called: player: {player?.Name}, killer: {killer?.Name}");
            if (killer == null || player == killer)
            {
                Team team = TeamList.GetPlayerTeam(player);
                if (team != null && team.Score > 0)
                    team.Score--;
            }
            else
            {
                Team playerTeam = TeamList.GetPlayerTeam(player);
                Team killerTeam = TeamList.GetPlayerTeam(killer);
                if (playerTeam != null && playerTeam == killerTeam) // Friendly fire
                {
                    if (playerTeam.Score > 0)
                        playerTeam.Score--;
                }
                else if (killerTeam != null)
                    killerTeam.Score++;
            }

            Rpc.Event(TDMEvents.UpdateScore).Trigger(TeamList.Teams[0].Score, TeamList.Teams[1].Score);

            if (tieBreaker && TeamList.Teams[0].Score != TeamList.Teams[1].Score)
                Events.Raise(ServerEvents.EndMission);
            else if(player != null)
                Rpc.Event(TDMEvents.Respawn).Trigger(player);           
        }

        public bool IsGameTied()
        {
            return TeamList.IsGameTied();
        }

        private bool IsValidTeamId(int team)
        {
            if (team < 0 || team >= TeamList.Teams.Count)
                return false;
            return true;
        }

        private SDK.Core.Models.Team GetMissionTeamFromGamemodeTeam(Team team)
        {
            return Mission.Teams[TeamList.Teams.IndexOf(team)];
        }
    }
}
