using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;

    public void InitializeCommands()
    {
        _conHost.RegisterCommand("forceartifactnode", "Forces an artifact to traverse to a given node", "forceartifacteffect <uid> <node ID>",
            ForceArtifactNode,
            ForceArtifactNodeCompletions);

        _conHost.RegisterCommand("getartifactmaxvalue", "Reports the maximum research point value for a given artifact", "forceartifacteffect <uid>",
            GetArtifactMaxValue);

        _conHost.RegisterCommand("randomizeartifact", "Randomize an artifact's node tree. Keep existing structure 'true' will randomize the trigger and effect of the nodes, while false will also randomize the number and structure of the entire tree.",
            "randomizeartifact <uid> [keep existing structure]",
            RandomizeArtifactCommand);

        _conHost.RegisterCommand("addartifactnode", "Adds a new node to an artifact node tree. If trigger or effect prototype ID is incorrect, will use defaults. To randomize trigger/effect, put the word 'random' instead of a prototype id or leave empty. Example use: addartifactnode 1234 555 TriggerBlood EffectChemicalPuddle", "addartifactnode <uid> <parent node ID> <trigger prototype id> <effect prototype id>",
            AddArtifactNode,
            AddArtifactNodeCompletions);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void ForceArtifactNode(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Argument length must be 2");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid) || !int.TryParse(args[1], out var id))
            return;

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        if (artifact.NodeTree.FirstOrDefault(n => n.Id == id) is { } node)
        {
            EnterNode(uid.Value, ref node);
        }
    }

    private CompletionResult ForceArtifactNodeCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.Components<ArtifactComponent>(args[0]), "<uid>");

        if (args.Length == 2 && NetEntity.TryParse(args[0], out var uidNet) && TryGetEntity(uidNet, out var uid))
        {
            if (TryComp<ArtifactComponent>(uid, out var artifact))
            {
                return CompletionResult.FromHintOptions(artifact.NodeTree.Select(s => s.Id.ToString()), "<node id>");
            }
        }

        return CompletionResult.Empty;
    }

    [AdminCommand(AdminFlags.Debug)]
    private void GetArtifactMaxValue(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
            shell.WriteError("Argument length must be 1");

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
            return;

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        var pointSum = GetResearchPointValue(uid.Value, artifact, true);
        shell.WriteLine($"Max point value for {ToPrettyString(uid.Value)} with {artifact.NodeTree.Count} nodes: {pointSum}");
    }

   [AdminCommand(AdminFlags.Fun)]
    private void AddArtifactNode(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 2 || args.Length > 4)
        {
            shell.WriteError("Requires between 2 and 4 arguments");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid) || !int.TryParse(args[1], out var id))
            return;

        var trigger = args.Length >= 3 ? args[2] : "random";
        var effect = args.Length == 4 ? args[3] : "random";

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        if (artifact.NodeTree.FirstOrDefault(n => n.Id == id) is { } node)
        {
            //Need to relearn what node IDs are used
            _usedNodeIds.Clear();
            foreach (var usedNode in artifact.NodeTree)
            {
                _usedNodeIds.Add(usedNode.Id);
            }

            var child = new ArtifactNode {Id = GetValidNodeId(), Depth = node.Depth + 1};
            node.Edges.Add(child.Id);
            child.Edges.Add(node.Id);
            artifact.NodeTree.Add(child);

            if (string.Equals(trigger, "random") || string.Equals(trigger, "'random'"))
                child.Trigger = GetRandomTrigger((EntityUid)uid, ref child);
            else
            {
                child.Trigger = trigger;
                _prototype.TryIndex<ArtifactTriggerPrototype>(trigger, out var maybeTrigger);
                if (maybeTrigger is null)
                {
                    shell.WriteLine($"{trigger} is not a valid prototype. Defaulting to {_defaultTrigger}");
                    child.Trigger = _defaultTrigger;
                }
            }
            if (string.Equals(effect, "random") || string.Equals(effect, "'random'"))
                child.Effect = GetRandomEffect((EntityUid)uid, ref child);
            else
            {
                child.Effect = effect;
                _prototype.TryIndex<ArtifactEffectPrototype>(effect, out var maybeEffect);
                if (maybeEffect is null)
                {
                    shell.WriteLine($"{effect} is not a valid prototype. Defaulting to {_defaultEffect}");
                    child.Effect = _defaultEffect;
                }
            }
            shell.WriteLine($"Created artifact with id: {child.Id}");
        }
        else
            shell.WriteError("Invalid node Id");
    }

    private CompletionResult AddArtifactNodeCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.Components<ArtifactComponent>(args[0]), "<uid>");
        if (args.Length == 2 && NetEntity.TryParse(args[0], out var uidNet) && TryGetEntity(uidNet, out var uid))
        {
            if (TryComp<ArtifactComponent>(uid, out var artifact))
            {
                return CompletionResult.FromHintOptions(artifact.NodeTree.Select(s => s.Id.ToString()), "<node id>");
            }
        }
        if (args.Length == 3)
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<ArtifactTriggerPrototype>(), "<trigger prototype>, also accepts 'random'");
        if (args.Length == 4)
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<ArtifactEffectPrototype>(), "<effect prototype>, also accepts 'random'");

        return CompletionResult.Empty;
    }

   [AdminCommand(AdminFlags.Fun)]
    private void RandomizeArtifactCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Argument length must be 2");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid) || !bool.TryParse(args[1], out var keepStructure))
            return;

        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        if (keepStructure)
        {
            //TODO save current node ID, exit current node, enter each node and randomly pick new trigger & effect, then re-enter saved ID.
        }
        else
        {
            //TODO exit current node, throw out entire tree, re-generate new tree, then enter the node.
        }
    }

}
