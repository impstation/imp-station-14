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
