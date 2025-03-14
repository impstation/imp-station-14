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
using Content.Server._Goobstation.Heretic.Components;
using Content.Shared.Humanoid;
using System.Collections.Immutable;
using Content.Shared.Mind;
using Robust.Shared.Random;
using Content.Server.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Server.Administration.Systems;


namespace Content.Server._Goobstation.Heretic.EntitySystems
{

    public sealed partial class HellWorldSystem:EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _ent = default!;

        private const string MapPath = "Maps/_Impstation/Ruins/cozy-radio-planetoid.yml"; //TODO replace this with hell world

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
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
         * da plan: make a species urist, gib it, move the target to a copy in hell world, then move them back with a HellVictimComponent and visual changes
         * hell returning will be here
         * 
         */

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            //hell world return
            var returnQuery = EntityQueryEnumerator<HellVictimComponent>();
            while (returnQuery.MoveNext(out var uid, out var victimComp))
            {
                //if they've been in hell long enough, return and revive them
                if (_timing.CurTime >= victimComp.ExitHellTime)
                {
                    _mind.TransferTo(victimComp.Mind, victimComp.OriginalBody);
                    //TODO: give the original body some visual changes
                    //TODO: brief the player on the fact they don't remember what happened. reference revolutionaryrulesystem line 246
                    _rejuvenate.PerformRejuvenate(uid);
                }
            }

        }

        public void AddVictimComponent(EntityUid victim)
        {
            EnsureComp<HellVictimComponent>(victim, out var victimComp);
            victimComp.OriginalBody = victim;
            victimComp.ExitHellTime = _timing.CurTime + victimComp.HellDuration;
            victimComp.OriginalPosition = Transform(victim).Coordinates;
            //make sure the victim has a mind
            if (!TryComp<MindContainerComponent>(victim, out var mindContainer) || !mindContainer.HasMind)
            {
                return;
            }
            victimComp.Mind = mindContainer.Mind.Value;

        }

        //AddVictimComponent MUST BE RUN BEFORE CALLING THIS!!
        public void SendToHell(EntityUid target, RitualData args, SpeciesPrototype species)
        {
            //TODO: add a check here so you can't send someone twice


            //get the hell victim component
            if (!args.EntityManager.TryGetComponent<HellVictimComponent>(target, out var victimComp))
                return;
            //get all possible spawn points, choose one, then get the place
            var spawnPoints = EntityManager.GetAllComponents(typeof(HellSpawnPointComponent)).ToImmutableList();
            var newSpawn = _random.Pick(spawnPoints);
            var spawnTgt = Transform(newSpawn.Uid).Coordinates;

            //spawn your hellsona
            var sufferingWhiteBoy = Spawn(species.Prototype, spawnTgt);
            _humanoid.CloneAppearance(victimComp.OriginalBody, sufferingWhiteBoy);

            //and then send the mind into the hellsona
            _mind.TransferTo(victimComp.Mind, sufferingWhiteBoy);
            victimComp.AlreadyHelled = true;

            //move the original body somewhere else
            TeleportRandomly(args, victimComp.OriginalBody);
            //returning the mind to the original body happens in Update()
        }

        //ported from funkystation
        private void TeleportRandomly(RitualData args, EntityUid uid) // start le teleporting loop -space
        {
            var maxrandomtp = 40; // this is how many attempts it will try before breaking the loop -space
            var maxrandomradius = 20; // this is the max range it will do -space


            if (!args.EntityManager.TryGetComponent<TransformComponent>(uid, out var xform))
                return;
            var coords = xform.Coordinates;
            var newCoords = coords.Offset(_random.NextVector2(maxrandomradius));
            for (var i = 0; i < maxrandomtp; i++) //start of the loop -space
            {
                var randVector = _random.NextVector2(maxrandomradius);
                newCoords = coords.Offset(randVector);
                if (!args.EntityManager.TryGetComponent<TransformComponent>(uid, out var trans))
                    continue;
                if (trans.GridUid != null && !_lookup.GetEntitiesIntersecting(newCoords.ToMap(_ent, _xform), LookupFlags.Static).Any()) // if they're not in space and not in wall, it will choose these coords and end the loop -space
                {
                    break;
                }
            }

            _xform.SetCoordinates(uid, newCoords);
        }
    }
}
