using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Server.GameObjects; //Imp - for PointLightSystem for radiator glow

namespace Content.Server.Atmos.EntitySystems;

public sealed class HeatExchangerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!; //imp

    float tileLoss;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatExchangerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);

        // Getting CVars is expensive, don't do it every tick
        Subs.CVar(_cfg, CCVars.SuperconductionTileLoss, CacheTileLoss, true);
    }

    private void CacheTileLoss(float val)
    {
        tileLoss = val;
    }

    private void OnAtmosUpdate(EntityUid uid, HeatExchangerComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        // make sure that the tile the device is on isn't blocked by a wall or something similar.
        if (args.Grid is {} grid
            && _transform.TryGetGridTilePosition(uid, out var tile)
            && _atmosphereSystem.IsTileAirBlocked(grid, tile))
        {
            return;
        }

        if (!_nodeContainer.TryGetNodes(uid, comp.InletName, comp.OutletName, out PipeNode? inlet, out PipeNode? outlet))
            return;

        var dt = args.dt;

        // Let n = moles(inlet) - moles(outlet), really a Δn
        var P = inlet.Air.Pressure - outlet.Air.Pressure; // really a ΔP
        // Such that positive P causes flow from the inlet to the outlet.

        // We want moles transferred to be proportional to the pressure difference, i.e.
        // dn/dt = G*P

        // To solve this we need to write dn in terms of P. Since PV=nRT, dP/dn=RT/V.
        // This assumes that the temperature change from transferring dn moles is negligible.
        // Since we have P=Pi-Po, then dP/dn = dPi/dn-dPo/dn = R(Ti/Vi - To/Vo):
        float dPdn = Atmospherics.R * (outlet.Air.Temperature / outlet.Air.Volume + inlet.Air.Temperature / inlet.Air.Volume);

        // Multiplying both sides of the differential equation by dP/dn:
        // dn/dt * dP/dn = dP/dt = G*P * (dP/dn)
        // Which is a first-order linear differential equation with constant (heh...) coefficients:
        // dP/dt + kP = 0, where k = -G*(dP/dn).
        // This differential equation has a closed-form solution, namely:
        float Pfinal = P * MathF.Exp(-comp.G * dPdn * dt);

        // Finally, back out n, the moles transferred in this tick:
        float n = (P - Pfinal) / dPdn;

        GasMixture xfer;
        if (n > 0)
            xfer = inlet.Air.Remove(n);
        else
            xfer = outlet.Air.Remove(-n);

        float CXfer = _atmosphereSystem.GetHeatCapacity(xfer, true);
        if (CXfer < Atmospherics.MinimumHeatCapacity)
            return;

        var radTemp = Atmospherics.TCMB;

        var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);
        bool hasEnv = false;
        float CEnv = 0f;
        if (environment != null)
        {
            CEnv = _atmosphereSystem.GetHeatCapacity(environment, true);
            hasEnv = CEnv >= Atmospherics.MinimumHeatCapacity && environment.TotalMoles > 0f;
            if (hasEnv)
                radTemp = environment.Temperature;
        }

        // How ΔT' scales in respect to heat transferred
        float TdivQ = 1f / CXfer;
        // Since it's ΔT, also account for the environment's temperature change
        if (hasEnv)
            TdivQ += 1f / CEnv;

        // Radiation
        float dTR = xfer.Temperature - radTemp;
        float dTRA = MathF.Abs(dTR);
        float a0 = tileLoss / MathF.Pow(Atmospherics.T20C, 4);
        // ΔT' = -kΔT^4, k = -ΔT'/ΔT^4
        float kR = comp.alpha * a0 * TdivQ;
        // Based on the fact that ((3t)^(-1/3))' = -(3t)^(-4/3) = -((3t)^(-1/3))^4, and ΔT' = -kΔT^4.
        float dT2R = dTR * MathF.Pow((1f + 3f * kR * dt * dTRA * dTRA * dTRA), -1f/3f);
        float dER = (dTR - dT2R) / TdivQ;
        _atmosphereSystem.AddHeat(xfer, -dER);
        if (hasEnv && environment != null)
        {
            _atmosphereSystem.AddHeat(environment, dER);

            // Convection

            // Positive dT is from pipe to surroundings
            float dT = xfer.Temperature - environment.Temperature;
            // ΔT' = -kΔT, k = -ΔT' / ΔT
            float k = comp.K * TdivQ;
            float dT2 = dT * MathF.Exp(-k * dt);
            float dE = (dT - dT2) / TdivQ;
            _atmosphereSystem.AddHeat(xfer, -dE);
            _atmosphereSystem.AddHeat(environment, dE);
        }

        if (n > 0)
            _atmosphereSystem.Merge(outlet.Air, xfer);
        else
            _atmosphereSystem.Merge(inlet.Air, xfer);

        UpdateRadiatorAppearance(uid, dER); //imp
    }

    //Imp edits below. Radiator glow.
    private void UpdateRadiatorAppearance(EntityUid uid, float radiatorEmittedEnergy)
    {

        // Log.Debug($"rad energy is {radiatorEmittedEnergy} for uid {uid}");

        //Return early if the pointlightcomponent doesn't exist (?)
        if (!_pointLight.TryGetLight(uid, out var pointLight))
            return;

        //Find the temperature of the radiator:
        //Assume that the radiator is made of stainless steel
        //Stainless steel has a heat capacity of about 502 J/(kg*K)
        //Assume the radiator is about 10 kg?
        //The radiator has a specific heat of 5020 J/K
        const float radiatorSpecificHeat = 5020;

        //Divide the energy emitted this atmos tick by the radiator's specific heat to get the radiator's temperature
        float radiatorTemperature = radiatorEmittedEnergy / radiatorSpecificHeat;
        // Log.Debug($"rad temp is {radiatorTemperature} for uid {uid}");

        //Find the color of the light based on the temperature of the radiator
        //Glowing starts at 798K (Draper point)
        //Disable the glow if the temperature is below this, and skip all future calculations
        if (radiatorTemperature < 798)
        {
            _pointLight.SetEnabled(uid, false, pointLight);
            return;
        }

        //Otherwise, start glowing
        _pointLight.SetEnabled(uid, true, pointLight);

        //Calculate the color of the radiator via Wien's displacement law (taken from Wikipedia)
        //For approximation, just get the peak wavelength emitted
        const float displacementConstant = 0.002898f;
        float peakWavelength = displacementConstant / radiatorTemperature;
        peakWavelength *= 1000000000; //Scale from meters to nanometers
        // Log.Debug($"rad wavelength is {peakWavelength}nm for uid {uid}");

        //Convert the wavelength to a usable color
        _pointLight.SetColor(uid, WavelengthToColor(peakWavelength), pointLight);
        // Log.Debug($"rad color is {WavelengthToColor(peakWavelength)} for uid {uid}");

        //Set the radiator's light intensity:
        //Per the Stefan–Boltzmann law, radiant exitance is proportional to the fourth power of the object's absolute temperature
        float SBConstant = 0.0567f; //Scaled up x 1000000 to prevent floating point errors, adjusted in the line below
        float radiantExitance = SBConstant * (float)Math.Pow(radiatorTemperature, 4) / 1000000;

        //Assuming the radiator is 1m^2, the radiant exitance is the wattage of light produced.
        //Now convert that to the energy and radius that robust toolbox wants
        Log.Debug($"rad radiant exitance is {radiantExitance} for uid {uid}");
        float energy = (20) / (1 + 100 * (float)Math.Pow(Math.E, -radiantExitance / 1000000));
        Log.Debug($"rad light energy is {energy} for uid {uid}");
        _pointLight.SetEnergy(uid, energy);
        _pointLight.SetRadius(uid, (float)Math.Sqrt(energy));

        //Update the sprite color of the radiator based ont he temperature of the radiator
    }

    private Color WavelengthToColor(float wavelength)
    {
        //Visible spectrum is from 380 nm to 750 nm
        float red = 0, green = 0, blue = 0;
        if (wavelength < 440)
        {
            red = 0.0f;
            green = 0.0f;
            blue = 1.0f;
        }
        else if (wavelength >= 380 && wavelength < 440) 
        {
            red = -(wavelength - 440) / (440 - 380);
            green = 0.0f;
            blue = 1.0f;
        }
        else if (wavelength >= 440 && wavelength < 490)
        {
            red = 0.0f;
            green = (wavelength - 440) / (490f - 440);
            blue = 1.0f;
        }
        else if (wavelength >= 490 && wavelength < 510)
        {
            red = 0.0f;
            green = 1.0f;
            blue = -(wavelength - 510) / (510 - 490);
        }
        else if (wavelength >= 510 && wavelength < 580)
        {
            red = (wavelength - 510) / (580 - 510);
            green = 1.0f;
            blue = 0.0f;
        }
        else if (wavelength >= 580 && wavelength < 645)
        {
            red = 1.0f;
            green = -(wavelength - 645) / (645 - 580);
            blue = 0.0f;
        }
        // else if (wavelength >= 645 && wavelength < 781) //Correct version
        else if (wavelength >= 645)
        {
            red = 1.0f;
            green = 0.0f;
            blue = 0.0f;
        }
        else
        {
            Log.Debug($"Color doesn't exist for {wavelength}");
        }
        // else
        // {
        //     red = 0.0f;
        //     green = 0.0f;
        //     blue = 0.0f;
        // }
        return new Color(red, green, blue);
    }
}