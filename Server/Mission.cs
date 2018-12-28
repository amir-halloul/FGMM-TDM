using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using FGMM.SDK.Server.Models;
using FGMM.SDK.Core.Models;

namespace FGMM.Gamemode.TDM.Server
{
    public class Mission : IMission
    {
        public string Name { get; set; }
        public string Gamemode { get; set; }
        public int Duration { get; set; }
        public SelectionData SelectionData { get; set; }
        public List<Team> Teams { get; set; }

        public static Mission Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Mission));
            using (StreamReader reader = new StreamReader(path))
            {
                Mission mission = (Mission)serializer.Deserialize(reader);
                reader.Close();
                mission.SelectionData.Teams = new List<string>();
                foreach (Team team in mission.Teams)
                    mission.SelectionData.Teams.Add(team.Name);
                return mission;
            }
        }

    }
}
