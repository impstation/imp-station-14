# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.

reagent-guidebook-status-effect = Causes { $effect } during metabolism{ $conditionCount ->
        [0] .
        *[other] {" "}when { $conditions }.
    }

reagent-effect-guidebook-modify-brain-damage-heals = { $chance ->
        [1] Heals { $amount } brain damage
   *[other] heal { $amount } brain damage
}
reagent-effect-guidebook-modify-brain-damage-deals = { $chance ->
        [1] Deals { $amount } brain damage
   *[other] deal { $amount } brain damage
}
reagent-effect-guidebook-modify-heart-damage-heals = { $chance ->
        [1] Heals { $amount } heart damage
   *[other] heal { $amount } heart damage
}
reagent-effect-guidebook-modify-heart-damage-deals = { $chance ->
        [1] Deals { $amount } heart damage
   *[other] deal { $amount } heart damage
}
reagent-effect-condition-guidebook-heart-damage = { $max ->
    [2147483648] it has at least {NATURALFIXED($min, 2)} heart damage
    *[other] { $min ->
                [0] it has at most {NATURALFIXED($max, 2)} heart damage
                *[other] it has between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} heart damage
             }
}
reagent-effect-guidebook-modify-brain-oxygen-heals = { $chance ->
        [1] Replenishes { $amount } brain oxygenation
   *[other] replenish { $amount } brain oxygenation
}
reagent-effect-guidebook-modify-brain-oxygen-deals = { $chance ->
        [1] Depletes { $amount } brain oxygenation
   *[other] deplete { $amount } brain oxygenation
}

reagent-effect-guidebook-start-heart = { $chance ->
        [1] Restarts the target's heart
   *[other] restart the target's heart
}
