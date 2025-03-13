using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Heretic.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class HellVictimComponent : Component
{
    [DataField]
    public bool AlreadyHelled = false;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitHellTime = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid OriginalBody;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid HellBody;
}
