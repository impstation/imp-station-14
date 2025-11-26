interaction-KissObject-name = Kiss
interaction-KissObject-description = Kiss this object, whatever melts the pain away.
interaction-KissObject-success-self-popup = You kiss {THE($target)}.
interaction-KissObject-success-target-popup = {THE($user)} kisses you.
interaction-KissObject-success-others-popup = {THE($user)} kisses {THE($target)}.

interaction-HugObject-name = Hug
interaction-HugObject-description = Hug this object. Hopefully it's at least warm.
interaction-HugObject-success-self-popup = You hug {THE($target)}.
interaction-HugObject-success-target-popup = {THE($user)} hugs you.
interaction-HugObject-success-others-popup = {THE($user)} hugs {THE($target)}.

interaction-PetObject-name = Pet
interaction-PetObject-description = Pet this object for the good job it surely just did.
interaction-PetObject-success-self-popup = You pet {THE($target)}.
interaction-PetObject-success-target-popup = {THE($user)} pets you on your head.
interaction-PetObject-success-others-popup = {THE($user)} pets {THE($target)}.

interaction-WaveAtObject-name = Wave at
interaction-WaveAtObject-description = Wave at the target. If you are holding an item, you will wave it.
interaction-WaveAtObject-success-self-popup = You wave {$hasUsed ->
    [false] at {THE($target)}.
    *[true] your {$used} at {THE($target)}.
}
interaction-WaveAtObject-success-target-popup = {THE($user)} waves {$hasUsed ->
    [false] at you.
    *[true] {POSS-PRONOUN($user)} {$used} at you.
}
interaction-WaveAtObject-success-others-popup = {THE($user)} waves {$hasUsed ->
    [false] at {THE($target)}.
    *[true] {POSS-PRONOUN($user)} {$used} at {THE($target)}.
}
