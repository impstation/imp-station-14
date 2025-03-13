using Content.Server.GameTicking.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Heretic.Components;
using Content.Shared.Heretic.Prototypes;

namespace Content.Server._Goobstation.Heretic.EntitySystems
{

    public sealed partial class HellWorldSystem:EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        private const string MapPath = "Maps/_Impstation/Ruins/cozy-radio-planetoid.yml"; //TODO replace this with hell world

        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Creates the hell world map.
        /// </summary>
        private void OnRoundStart(RoundStartingEvent ev)
        {
            _map.CreateMap(out var mapId);
            var options = new MapLoadOptions { LoadMap = true };
            if (_mapLoader.TryLoad(mapId, MapPath, out _, options))
                _map.SetPaused(mapId, false);
        }

        //WHATS NEEDED:
        /* update() - take someone out of hell if needed
         * hell sending can be in the sacrifice system
         * da plan: make a species urist, gib it, move the target to hell world, then move them back with a HellVictimComponent and visual changes
         * hell returning will be here
         * 
         */

        public override void Update(float frameTime) 
        {
            base.Update(frameTime);

            //hell world return, shamelessly stolen from cosmic shunt
            var returnQuery = EntityQueryEnumerator<HellVictimComponent>();
            while (returnQuery.MoveNext(out var uid, out var comp))
            {
                //if they've been in hell long enough, return them
                if (_timing.CurTime >= comp.ExitHellTime)
                {
                    //TODO: this
                }
            }

        }

        //HELLVICTIMCOMPONENT MUST BE GIVEN BEFORE CALLING THIS!!
        private void SendToHell(Entity<HellVictimComponent> victim, RitualData args)
        {

        }
    }
}
