using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Impstation.Nutrition.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Queries.Queries;
using Content.Server.NPC.Systems;
using Content.Server._Impstation.AnimalHusbandry.Components;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Operator for a mob to find an eligible breeding partner of the same breeding group.
/// </summary>
public sealed partial class BreedingUtilityOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    private AnimalHusbandrySystemImp _breedSystem = default!;

    [DataField("key")] public string Key = "Target";

    [DataField("keyCoordinates")] public string KeyCoordinates = "TargetCoordinates";

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _breedSystem = sysManager.GetEntitySystem<AnimalHusbandrySystemImp>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager);

        // Let's make sure it's something that can even reproduce first
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp))
            return (false, null);

        var result = _entManager.System<NPCUtilitySystem>().GetEntities(blackboard, Prototype);
        var score = -float.MaxValue;
        EntityUid? target = null;

        //for every target
        foreach (var potentialTarget in result.Entities.Keys)
        {
            //if it doesn't exist, doesn't have a repro comp or isn't in our repro group, continue
            if (potentialTarget == EntityUid.Invalid ||
                !_entManager.TryGetComponent<ImpReproductiveComponent>(potentialTarget, out var comp))
                continue;

            // Checks if we are in the list of valid mob partners
            if (!IsValidPartner(comp, reproComp.MobType))
                continue;

            // Checking if the other mob reaches breeding conditions
            if (!_breedSystem.CanYouBreed((potentialTarget, comp)))
                continue;

            // Making sure two of the same gender aren't trying to breed
            // If an animal is Agender then this doesn't matter
            if (reproComp.Gender != AnimalGender.Agender && reproComp.Gender == comp.Gender)
                continue;

            //if it's score is less than our current score, continue
            var targScore = result.Entities[potentialTarget];
            if (targScore < score)
                continue;

            //finally, update our score and set the new target
            score = targScore;
            target = potentialTarget;
        }

        //we didn't find a valid target, so don't plan anything
        if (target == null)
            return (false, null);

        //we found a valid target, so return the plan
        return (true, new Dictionary<string, object>()
        {
            {Key, target.Value!},
            {KeyCoordinates, new EntityCoordinates(target.Value!, Vector2.Zero)}
        });
    }

    public bool IsValidPartner(ImpReproductiveComponent comp, string self)
    {
        if(comp.ValidPartners.Count == 0 || self == "")
            return true;

        foreach(var partner in comp.ValidPartners)
        {
            if (partner == self) return true;
        }

        return false;
    }

    #region OVERRIDES
    // These exist to make the blackboard happy. Do not think about them.
    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
    }

    public void Shutdown(NPCBlackboard blackboard)
    {
        //blackboard.Remove<EntityUid>(Key);
        blackboard.Remove<EntityCoordinates>(KeyCoordinates);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);
        Shutdown(blackboard);
    }

    // Keep track of the beast we're breeding until one of us is pregnant or
    // the elapsed time is up
    /*
     * %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%#%##I DON'T KNOOOOOOOOOOOOOOOOOOOOOOOOOW-%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
  ..................................................................................................
                                                                                                    
                                                                                                    
                                                                                                    
                                                                                                    
                                                                                                    
                                                                                                    
                                                                                                    
                                              .  ..  ..    ....=+++***=...                          
                                           ...+..-.:..:::-*%%%%%%%######*=...                       
                                      ...:--==##++*####%%##%%%%%%%%%%#######+:.                     
                                    ..==+*####%%%%%%%%%%%%%%%%%%@%%%#####*#####.                    
                                   .-+*#%%%%%@%%%%%%%%%@@@@@%@%%%%%********######-.                 
                                 .=+*#%%%%%@@%%%%%%%%%@@@@@@@@%%%%%%#%##**##***####+.               
                                .:=##%%%%%%%%%%%%%%%%%%%@@@@@@@@@@@@@%%#############*+.             
                              ..=#***##***#@#%%%%%@%@%@@%@@@@@@@@@@@@%%%%####*********+:.           
                           ..-********######%@@@@@@@%%%@@@@@@@@@@@@@@@@@@%######**#****+-.          
                          .:##++++*#@@*###%@%@@@@@@@@@@@@%@%##%%%#%@@@@@@%%%##*****###**+=..        
                         :*+++=++#@%+***#***%@@@@@@@@@@@@%#%@%%@@%#@@@@@%@%####************=.       
                        :+*+=+++*#++*##%%@%%%@@@@@@@@@@@@##%@@%#%%#@@@@@@@%%%##*******++++++=-.     
                       :+*++=++*++##@%+*##%%@@@@@@@@@@@@@%##%#%#%%#@@@@@%@@@%#####***++*++***+*:.   
                      .+*+*+++++**#*+*##%%#%%@@%%%@@@@@@@@@##*###%%@@@@@@%%@@%#%##********+++****-. 
                    .-+*******+****####@@@@@@@%%%%%@@@@@@@@%%*#%@@@@%@@%@@%%%@@####**********+***#*:
                    -++****######*##@@@@@@@@%%@@@@@@@@@@@%%@@%%%%@@@@%%%%%%%%%@%%%###***********####
                   .=++*****####%%%@@@@@@@@%%%%%@%@@%@@@@@@@@@@%%@@@@%%%%%%%##%%@@@###*******#**#%%%
                  .=++****#####%%%@@@@@@@@@@%%##%%%%%@%@@@@@@@@@%@@@@@%@#%#%###%@@@%%####*##*#######
                ..=******###%%##@@@%@@@@@%%%##****##%%%@@@@@@@@%%@@@%%@%%#######%@@@@%%%##**##%#%%%#
 ................=+*##**####%##%%%%%@@@@%%####%%%##%%%%@@%%@@@%%@%%@%%%%%######%%%@@@@@%%###%@@%@@%%
 ..................................=%%%%###%%@@@%@@@@@%%%##%@@@@@@@%@%%%%%######%%%%@@@@%%%@@@@@@@%%
 ...................................+%%@@@@@%%%#########%%%%@@@@@@@@%@%%%%%%%%%%%###%%@@@@@@@@@%####
  ...................................%@@@@@@@+*#*############%@@@@@@%@@%%%%%%%%%%%%%%%%%%@@@@@@@%@@%
   ...................................@@@@@@@+*#@*#@##########@@@@%@@@@@%@%%#*###%%%#%%%#%@@@@%%@%%@
   ...................................+%@@@@@@+##############%@@@@%@%%%###%%%@%####%%%%%#@@@@%%%%%%%
    ...................................%@@@@@@%*#############@%@@@%@@%%###+*##%%%%%%%@@@@@%######%%%
     ...................................@@@@@@@@**#####%#*####***%@%%%%%%#****#####%@@@###********##
     ...................................*@@@@@@@%#+##**+############%#%%%%%#######%@@%%%%######%%%%%
      ...................................%@@@@@@@%%##%%%%%##%#**#*##%%**#%%****#%@@@%%%%%####%%%%%##
       ..................................:@@@@@@@@@@%%%%%#####**#####%%###%%%%%@@%%####%%%%######*+*
       ...................................*%%%@@@@@@%%%%#######*#**###%%%%%%%%%%%%%%%%%######*#%####
        ...................................%%%%@@@@@@@@%%%%%######***%%%%%###%#%%%%%%%%%%#%%%%%#%%%%
         ..................................:%%%@@@@@@@@@@%%%%%%%%%%%%%########%%%%%%%%@%####%%%%%@@@
          ..................................*%%%@@%%@@@@@@@@%%%%%%%####%%%%%%%%%%#*####%%%##%@@@@@@@
          ...................................##@@@@%@@@@@@@@@%%@%%#%%%%%%#%#%@@@@@%###%@@@@@@@@@@@@@
          ...................................-#%@@@@@%@@@@@@@@@#%##%%#%#%%@%%@@@@@@@@@@@@@@@@@@@@@@%
           ...................................#%@@@@@%@@@@@@@@%%%%%%%#%#%#@@%@@@@@@%%%%%%%%@@@@@@@@%
           .-..................................%%%@%%%%@@@@%%#%#%#%%##%##%%@@@%%#%#####%#%%%@@@@@@%#
            .:.................................=%%@%%%@@%%%##%@@%%%#%%%##%%@%####%####%%%%%%@@@@@###
.    . .    .-..................................+++****#*##%%@@@%%#%%###%%%@%#########%%%%%@@@@%%###
     */
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // If either of us have lost our breedability or just outright stopped existing
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp)
            || !blackboard.TryGetValue<EntityUid>(Key, out var target, _entManager)
            || !_entManager.TryGetComponent<ImpReproductiveComponent>(target, out var targetComp)
            || target == EntityUid.Invalid)
        {
            return HTNOperatorStatus.Failed;
        }

        // Have we done our job? Or have we waited too long
        if (targetComp.Pregnant
            || reproComp.Pregnant)
            //|| reproComp.NextSearch > _time.CurTime)
        {
            return HTNOperatorStatus.Continuing;
        }

        return HTNOperatorStatus.Finished;
    }
    #endregion

}
