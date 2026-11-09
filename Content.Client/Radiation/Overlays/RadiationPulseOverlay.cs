using System.Numerics;
using Content.Shared.Radiation.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Impstation.CCVar; // imp
using Content.Shared._Impstation.Radiation.Events; // imp
using Robust.Shared.Configuration; // imp

namespace Content.Client.Radiation.Overlays
{
    public sealed class RadiationPulseOverlay : Overlay, IEntityEventSubscriber // imp, added IEntityEventSubscriber
    {
        private static readonly ProtoId<ShaderPrototype> RadiationShader = "Radiation";

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!; // imp
        private TransformSystem? _transform;

        private const float MaxDist = 15.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _baseShader;
        private readonly Dictionary<NetEntity, (ShaderInstance shd, RadiationShaderInstance instance)> _pulses = new(); // imp, changed from EntityUid to NetEntity
        private bool _simplifiedRadiationPulse; // imp

        public RadiationPulseOverlay()
        {
            IoCManager.InjectDependencies(this);
            _baseShader = _prototypeManager.Index(RadiationShader).Instance().Duplicate();

            _entityManager.EventBus.SubscribeEvent<RadiationPulseInRangeEvent>(EventSource.Network, this, OnRadiationPulseInRangeEvent); // imp
            _configManager.OnValueChanged(ImpCCVars.EnableSimplifiedRadiationPulse, value => _simplifiedRadiationPulse = value, true); // imp
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            RadiationQuery(args); // imp, changed args.Viewport.Eye to args
            return _pulses.Count > 0;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null || args.Viewport.Eye == null) // imp, added || args.Viewport.Eye == null
                return;

            var worldHandle = args.WorldHandle;
            var viewport = args.Viewport;

            foreach ((var shd, var instance) in _pulses.Values)
            {
                if (instance.CurrentMapCoords.MapId != args.MapId)
                    continue;

                // imp start
                var life = (_gameTiming.RealTime - instance.Start).TotalSeconds / instance.Duration;

                if (_simplifiedRadiationPulse)
                {
                    // mirroring what is done in the shader without all the noise
                    var nlife = MathF.Pow(MathF.Sin((float)life * MathF.PI), 0.5f);
                    var currentRadius = instance.Range * nlife;

                    worldHandle.DrawCircle(instance.CurrentMapCoords.Position, currentRadius, Color.LimeGreen.WithAlpha(nlife * 0.9f));
                    worldHandle.DrawCircle(instance.CurrentMapCoords.Position, currentRadius, Color.LimeGreen.WithAlpha(nlife), false);
                }
                else // imp end
                {
                    // To be clear, this needs to use "inside-viewport" pixels.
                    // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                    var tempCoords = viewport.WorldToLocal(instance.CurrentMapCoords.Position);
                    tempCoords.Y = viewport.Size.Y - tempCoords.Y;
                    shd?.SetParameter("renderScale", viewport.RenderScale * viewport.Eye.Scale); // imp, added * viewport.Eye.Scale
                    shd?.SetParameter("positionInput", tempCoords);
                    shd?.SetParameter("range", instance.Range);
                    /* imp, moved out of the if else
                    var life = (_gameTiming.RealTime - instance.Start).TotalSeconds / instance.Duration;
                    */
                    shd?.SetParameter("life", (float)life);
                    // There's probably a very good reason not to do this.
                    // Oh well!
                    shd?.SetParameter("SCREEN_TEXTURE", viewport.RenderTarget.Texture);

                    worldHandle.UseShader(shd);
                    worldHandle.DrawRect(Box2.CenteredAround(instance.CurrentMapCoords.Position, new Vector2(instance.Range, instance.Range) * 2f), Color.White);
                }
            }

            worldHandle.UseShader(null);
        }

        //Queries all pulses on the map and either adds or removes them from the list of rendered pulses based on whether they should be drawn (in range? on the same z-level/map? pulse entity still exists?)
        private void RadiationQuery(in OverlayDrawArgs args) // imp, changed IEye? currentEye to in OverlayDrawArgs args
        {
            _transform ??= _entityManager.System<TransformSystem>();

            if (args.Viewport.Eye == null) // imp, changed currentEye to args.Viewport.Eye
            {
                _pulses.Clear();
                return;
            }

            /* imp removal
            var currentEyeLoc = currentEye.Position;
            */

            var pulses = _entityManager.EntityQueryEnumerator<RadiationPulseComponent>();
            //Add all pulses that are not added yet but qualify
            while (pulses.MoveNext(out var pulseEntity, out var pulse))
            {
                var netEnt = _entityManager.GetNetEntity(pulseEntity); // imp
                if (!_pulses.ContainsKey(netEnt) && PulseQualifies(pulseEntity, args.WorldAABB, args.MapId)) // imp, changed pulseEntity to netEnt and the variables PulseQualifies takes in to match imp changes
                {
                    _pulses.Add(
                            netEnt, // imp, changed from pulseEntity to netEnt
                            (
                                _baseShader.Duplicate(),
                                new RadiationShaderInstance(
                                    _transform.GetMapCoordinates(pulseEntity),
                                    pulse.VisualRange,
                                    pulse.StartTime,
                                    pulse.VisualDuration,
                                    TimeSpan.Zero // imp
                                )
                            )
                    );
                }
            }

            var activeShaderIds = _pulses.Keys;
            foreach (var pulseEntity in activeShaderIds) //Remove all pulses that are added and no longer qualify
            {
                if (// _entityManager.EntityExists(pulseEntity) && imp removal
                    _entityManager.TryGetEntity(pulseEntity, out var ent) && // imp
                    PulseQualifies(ent.Value, args.WorldAABB, args.MapId) && // imp, changed variables PulseQualifies takes in to match imp changes
                    _entityManager.TryGetComponent(ent, out RadiationPulseComponent? pulse)) // imp, changed pulseEntity to ent
                {
                    var shaderInstance = _pulses[pulseEntity];
                    shaderInstance.instance.CurrentMapCoords = _transform.GetMapCoordinates(ent.Value); // imp, changed from pulseEntity to ent.Value
                    shaderInstance.instance.Range = pulse.VisualRange;
                }
                else if (_pulses.TryGetValue(pulseEntity, out var value) // imp added if (_pulses.TryGetValue(pulseEntity, out var value)
                        && value.instance.NextExpectedServerUpdate < _gameTiming.RealTime) // imp
                {
                    _pulses[pulseEntity].shd.Dispose();
                    _pulses.Remove(pulseEntity);
                }
            }

        }

        private bool PulseQualifies(EntityUid pulseEntity, Box2 worldAABB, MapId mapId) // imp, changed from MapCoordinates currentEyeLoc to Box2 worldAABB, MapId mapId
        {
            var transformComponent = _entityManager.GetComponent<TransformComponent>(pulseEntity);
            var transformSystem = _entityManager.System<SharedTransformSystem>();
            /* imp removal, using SingularityOverlay logic instead
            return transformComponent.MapID == currentEyeLoc.MapId
                && transformSystem.InRange(transformComponent.Coordinates, transformSystem.ToCoordinates(transformComponent.ParentUid, currentEyeLoc), MaxDist);
            */

            // imp start
            if (transformComponent.MapID != mapId)
                return false;

            var mapPos = transformSystem.GetWorldPosition(transformComponent);
            return (mapPos - worldAABB.ClosestPoint(mapPos)).LengthSquared() <= MaxDist * MaxDist;
            // imp end
        }

        // imp start
        private void OnRadiationPulseInRangeEvent(ref RadiationPulseInRangeEvent args)
        {
            if (_pulses.TryGetValue(args.Pulse, out var value))
            {
                value.instance.CurrentMapCoords = args.Coords;
                value.instance.Range = args.VisualRange;
                value.instance.Duration = args.VisualDuration;
                value.instance.NextExpectedServerUpdate = args.NextExpectedUpdate;
            }
            else
                _pulses.Add(args.Pulse, (_baseShader.Duplicate(), new RadiationShaderInstance(args.Coords, args.VisualRange, args.StartTime, args.VisualDuration, args.NextExpectedUpdate)));
        }
        // imp end

        private sealed record RadiationShaderInstance(MapCoordinates CurrentMapCoords, float Range, TimeSpan Start, float Duration, TimeSpan NextExpectedServerUpdate) // imp added TimeSpan NextExpectedServerUpdate
        {
            public MapCoordinates CurrentMapCoords = CurrentMapCoords;
            public float Range = Range;
            public TimeSpan Start = Start;
            public float Duration = Duration;
            public TimeSpan NextExpectedServerUpdate = NextExpectedServerUpdate; // imp
        };
    }
}

