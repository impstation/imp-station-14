using Content.Server._Goobstation.Heretic.Components;
using Content.Server._Goobstation.Heretic.UI;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Server.Heretic.Components;
using Content.Server.Humanoid;
using Content.Server.StationEvents;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Immutable;
using Content.Server.Cloning;
using Content.Shared.Administration.Systems;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Body.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Tag;

//this is kind of badly named since we're doing infinite archives stuff now but i dont feel like changing it :)

namespace Content.Server._Goobstation.Heretic.EntitySystems
{

    public sealed class HellWorldSystem : EntitySystem
    {
        [Dependency] private readonly BlindableSystem _blind = default!;
        [Dependency] private readonly EuiManager _euiMan = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
        [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly CloningSystem _cloning = default!;
        [Dependency] private readonly SharedBodySystem _body = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        private readonly ResPath _mapPath = new("Maps/_Impstation/Nonstations/InfiniteArchives.yml");
        private readonly ProtoId<CloningSettingsPrototype> _cloneSettings = "HellClone";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HellVictimComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<InHellComponent, HereticBeforeHellEvent>(BeforeSend);
            SubscribeLocalEvent<InHellComponent, HereticSendToHellEvent>(OnSend);
            SubscribeLocalEvent<InHellComponent, HereticReturnFromHellEvent>(OnReturn);
        }

        /// <summary>
        /// Creates the hell world map.
        /// </summary>
        public void MakeHell()
        {
            if (_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
                _map.SetPaused(map.Value.Comp.MapId, false);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            //hell world return
            var returnQuery = EntityQueryEnumerator<InHellComponent>();
            while (returnQuery.MoveNext(out var uid, out var hellComp))
            {
                if(_timing.CurTime >= hellComp.ExitHellTime)
                {
                    RaiseLocalEvent(uid, new HereticReturnFromHellEvent());
                }
            }
        }

        //set up all the info that's needed for the hell trip 
        private void BeforeSend(Entity<InHellComponent> uid, ref HereticBeforeHellEvent args)
        {
            //spawn a clone of the victim
            _cloning.TryCloning(uid, _transform.GetMapCoordinates(uid), uid.Comp.CloneSettings, out var clone);

            //gib clone to get matching organs.
            if (clone != null)
                _body.GibBody(clone.Value, true);

            //teleport the body to a midround antag spawn spot so it's not just tossed into space
            TeleportToHereticSpawnPoint(uid);
            uid.Comp.OriginalBody = uid;
            uid.Comp.ExitHellTime = _timing.CurTime + uid.Comp.HellDuration;
            uid.Comp.OriginalPosition = Transform(uid).Coordinates;
            //make sure the victim has a mind
            if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || !mindContainer.HasMind)
            {
                return;
            }
            uid.Comp.HasMind = true;
            uid.Comp.Mind = mindContainer.Mind.Value;
        }

        private void OnSend(Entity<InHellComponent> uid, ref HereticSendToHellEvent args)
        {
            var inHell = EnsureComp<InHellComponent>(uid);

            //get all possible spawn points, choose one, then get the place
            var spawnPoints = EntityManager.GetAllComponents(typeof(HellSpawnPointComponent)).ToImmutableList();
            var newSpawn = _random.Pick(spawnPoints);

            //if there is no mind (e.g. salvage corpse), don't bother with juggling all this crap
            if (!inHell.HasMind || inHell.Mind == null)
            {
                SacrificeCleanup(uid);
                return;
            }

            //get mind, keep it from escaping my cutscene
            var mindComp = Comp<MindComponent>(inHell.Mind.Value);
            mindComp.PreventGhosting = true;

            //make clone 
            _cloning.TryCloning(uid, _xform.GetMapCoordinates(newSpawn.Uid), _cloneSettings, out var clone); //RIP SacrifialWhiteBoy variable name

            if (TryComp<BlindableComponent>(clone, out _))
            {
                _blind.AdjustEyeDamage(clone.Value, 5); //make it more disorienting

            }

            //and then send the mind into the hellsona
            _mind.TransferTo(inHell.Mind.Value, clone);

            //add the victim & resacrifice comp to the original body
            SacrificeCleanup(uid);
        }
        private void OnReturn(Entity<InHellComponent> uid, ref HereticReturnFromHellEvent args)
        {
            if (!TryComp<InHellComponent>(uid, out var inHell))
            {
                return;
            }
            if (!inHell.HasMind || inHell.Mind == null)
            {
                return;
            }

            //put them back in the original body
            _mind.TransferTo(inHell.Mind.Value, inHell.OriginalBody);

            //let them ghost again
            var mindComp = Comp<MindComponent>(inHell.Mind.Value);
            mindComp.PreventGhosting = false;

            //tell them about the metashield
            if (_playerManager.TryGetSessionById(mindComp.UserId, out var session))
                _euiMan.OpenEui(new HellMemoryEui(), session);

            //cleanup so they don't get in here again
            RemComp<InHellComponent>(uid);
        }

        //add NoSacrificeComp so they can't be sac'd again
        private void SacrificeCleanup(EntityUid uid)
        {
            EnsureComp<NoSacrificeComponent>(uid);
            EnsureComp<HellVictimComponent>(uid, out var victim);
            TransformVictim(uid);
            _rejuvenate.PerformRejuvenate(uid);
            AddHellTrait(uid, victim);
        }

        public void TeleportToHereticSpawnPoint(EntityUid uid)
        {
            //get all possible spawn points, choose one, then get the place
            var spawnPoints = EntityManager.GetAllComponents(typeof(MidRoundAntagSpawnLocationComponent)).ToImmutableList();
            if (spawnPoints.Count == 0)
            {
                //fallback to cryo, incase someone forgot to map points
                spawnPoints = EntityManager.GetAllComponents(typeof(CryostorageComponent)).ToImmutableList();
            }
            var newSpawn = _random.Pick(spawnPoints);
            var spawnTgt = Transform(newSpawn.Uid).Coordinates;

            _xform.SetCoordinates(uid, spawnTgt);
        }

        private void AddHellTrait(EntityUid uid, HellVictimComponent victim)
        {
            //get a random trait from the list
            var traitId = _prototypeManager.Index(victim.Traits[_random.Next(victim.Traits.Count)]);

            //add it
            // Add all components required by the prototype to the body or specified organ
            if (traitId.Organ != null)
            {
                foreach (var organ in _body.GetBodyOrgans(uid))
                {
                    if (traitId.Organ is { } organTag && _tag.HasTag(organ.Id, organTag))
                    {
                        EntityManager.AddComponents(organ.Id, traitId.Components);
                    }
                }
            }
            else
            {
                EntityManager.AddComponents(uid, traitId.Components, false);
            }
        }

        private void TransformVictim(EntityUid ent)
        {
            if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            {
                //there's no color saturation methods so you get this garbage instead
                var skinColor = humanoid.SkinColor;
                var colorHSV = Color.ToHsv(skinColor);
                colorHSV.Y /= 4;
                var newColor = Color.FromHsv(colorHSV);
                //make them look like they've seen some shit
                _humanoid.SetSkinColor(ent, newColor, true, false, humanoid);
                _humanoid.SetBaseLayerColor(ent, HumanoidVisualLayers.Eyes, Color.White, true, humanoid);
            }
        }

        private void OnExamine(Entity<HellVictimComponent> ent, ref ExaminedEvent args)
        {
            args.PushMarkup($"[color=red]{Loc.GetString("heretic-hell-victim-examine", ("ent", args.Examined))}[/color]");
        }
    }
}
