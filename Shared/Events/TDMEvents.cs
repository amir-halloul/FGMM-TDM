namespace FGMM.Gamemode.TDM.Shared.Events
{
    public class TDMEvents
    {
        public const string Spawn = "fgmm:tdm:spawn";
        public const string Respawn = "fgmm:tdm:respawn";
        public const string RequestRespawnData = "fgmm:tdm:respawn:data:request";

        public const string UpdateScore = "fgmm:tdm:ui:update:score";
        public const string UpdateTimer = "fgmm:tdm:ui:update:timer";

        public const string GetPlayerTeam = "fgmm:tdm:player:team";
        public const string PlayerAdded = "fgmm:tdm:player:team:added";
    }
}
