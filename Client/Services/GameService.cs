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
    class GameService : Service
    {
        private const int RespawnTime = 5000; // Time before respawning in ms
        private int RemainingTime = 0;
        private int TimeRecieved;
        private bool GameRunning = false;

        public GameService(ILogger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager) : base(logger, events, rpc, tickManager)
        {
            Logger.Info("Game service started.");

            Rpc.Event(TDMEvents.Spawn).On<SpawnData>(OnSpawnRequested);
            Rpc.Event(TDMEvents.UpdateScore).On<int, int>(OnScoreUpdated);
            Rpc.Event(TDMEvents.UpdateTimer).On<int>(OnTimerUpdated);
            Rpc.Event(TDMEvents.Respawn).On(OnRespawnRequested);

            tickManager.Attach(MissionTimerTick);
        }

        public void Start()
        {
            GameRunning = true;
        }

        public void Stop()
        {
            GameRunning = false;
            ToggleScoreHud(false);
        }

        private async Task MissionTimerTick()
        {
            if (RemainingTime > 0)
            {
                UpdateTime(RemainingTime - (int)Math.Floor((double)(API.GetGameTimer() - TimeRecieved) / 1000));
            }
            await BaseScript.Delay(1000);
        }

        private async void OnRespawnRequested(IRpcEvent rpc)
        {
            await BaseScript.Delay(RespawnTime);
            if (!GameRunning)
                return;
            SpawnData respawnData = await Rpc.Event(TDMEvents.RequestRespawnData).Request<SpawnData>();
            await SpawnPlayer(respawnData, true);
        }

        private async void OnSpawnRequested(IRpcEvent rpc, SpawnData data)
        {
            if (!GameRunning)
                return;
            await SpawnPlayer(data);
        }

        private void OnTimerUpdated(IRpcEvent rpc, int time)
        {
            RemainingTime = time;
            TimeRecieved = API.GetGameTimer();
            UpdateTime(RemainingTime);
        }

        private void OnScoreUpdated(IRpcEvent rpc, int score1, int score2)
        {
            UpdateScores(score1, score2);
        }  

        private async Task SpawnPlayer(SpawnData data, bool respawn = false)
        {
            Screen.Fading.FadeOut(0);

            API.RenderScriptCams(false, false, 0, false, false);

            Game.PlayerPed.Resurrect();
            Game.PlayerPed.ClearBloodDamage();

            if (!respawn)
            {
                while (!await Game.Player.ChangeModel(new Model((PedHash)Enum.Parse(typeof(PedHash), data.Skin, true))))
                    await BaseScript.Delay(500);
                API.SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
            }               

            EquipLoadout(data.Loadout);

            Game.PlayerPed.PositionNoOffset = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
            Game.PlayerPed.Rotation = new Vector3(0, 0, data.Position.A);
            API.RequestCollisionAtCoord(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z);
            while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                await BaseScript.Delay(100);

            Game.Player.CanControlCharacter = true;
            Game.PlayerPed.IsPositionFrozen = false;

            ToggleScoreHud(true);   

            API.SetEntityAlpha(Game.PlayerPed.Handle, 150, 0);
            Game.PlayerPed.IsInvincible = true;
            Screen.Fading.FadeIn(1000);           
            await BaseScript.Delay(3000);
            API.ResetEntityAlpha(Game.PlayerPed.Handle);
            Game.PlayerPed.IsInvincible = false;     
        }

        private void EquipLoadout(List<SDK.Core.Models.Weapon> loadout)
        {
            Game.PlayerPed.Weapons.RemoveAll();

            foreach (SDK.Core.Models.Weapon weapon in loadout)
                Game.PlayerPed.Weapons.Give((WeaponHash)Enum.Parse(typeof(WeaponHash), weapon.Hash, true), (int)weapon.Ammo, false, true);

            Game.PlayerPed.Weapons.Select(Game.PlayerPed.Weapons.BestWeapon);
        }

        private void ToggleScoreHud(bool toggle)
        {
            Serializer serializer = new Serializer();
            Dictionary<string, object> message = new Dictionary<string, object>()
            {
                { "type", "ToggleScoreHud"},
                { "toggle", toggle}
            };
            API.SendNuiMessage(serializer.Serialize(message));
        }

        private void UpdateScores(int score1, int score2)
        {
            Serializer serializer = new Serializer();
            Dictionary<string, object> message = new Dictionary<string, object>()
            {
                { "type", "UpdateScores"},
                { "score1", score1},
                { "score2", score2}
            };
            API.SendNuiMessage(serializer.Serialize(message));
        }

        private async void UpdateTime(int seconds)
        {
            Serializer serializer = new Serializer();
            Dictionary<string, object> message = new Dictionary<string, object>()
            {
                { "type", "UpdateTime"},
                { "time", FormatSeconds(seconds)}
            };
            API.SendNuiMessage(serializer.Serialize(message));
        }

        private string FormatSeconds(int seconds)
        {
            if (seconds <= 0)
                return "00:00";
            int minutes = seconds / 60;
            seconds -= minutes * 60;

            return $"{minutes.ToString("D2")}:{seconds.ToString("D2")}";
        }
    }
}
