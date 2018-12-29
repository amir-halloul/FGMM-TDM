using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FGMM.SDK.Client.Diagnostics;
using FGMM.SDK.Client.Gamemodes;
using FGMM.SDK.Core.Models;
using FGMM.SDK.Client.Events;
using FGMM.SDK.Client.RPC;
using FGMM.Gamemode.TDM.Client.Services;

namespace FGMM.Gamemode.TDM.Client
{
    public class TDM : IGamemode
    {
        private Logger Logger { get; set; }
        private IEventManager Events { get; set; }
        private IRpcHandler Rpc { get; set; }
        private ITickManager TickManager { get; set; }

        GameService GameService;

        public TDM(Logger logger, IEventManager events, IRpcHandler rpc, ITickManager tickManager)
        {
            Logger = logger;
            Events = events;
            Rpc = rpc;
            TickManager = tickManager;
            GameService = new GameService(new Logger("TDM | GameService"), Events, Rpc, TickManager);
        }

        public void Start()
        {
            Logger.Info("TDM gamemode started.");            
        }

        public void Stop()
        {
            Logger.Info("TDM gamemode stopped.");
        }
    }
}
