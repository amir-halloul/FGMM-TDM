using System.Collections.Generic;
using FGMM.SDK.Core.Models;

namespace FGMM.Gamemode.TDM.Shared.Models
{
    public class SpawnData
    {
        public Position Position { get; set; }

        public string Skin { get; set; }

        public List<Weapon> Loadout { get; set; }

        public SpawnData()
        {

        }
    }
}
