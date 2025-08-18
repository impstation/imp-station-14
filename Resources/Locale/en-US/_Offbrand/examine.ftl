wound-bleeding-modifier = [color=red]bleeding {$wound}[/color]
wound-tended-modifier = tended {$wound}
wound-bandaged-modifier = bandaged {$wound}
wound-salved-modifier = salved {$wound}

wound-count-modifier =
    { CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $count ->
        [1] {INDEFINITE( $wound )} { $wound }
        [2] two { $wound }s
        [3] a few { $wound }s
        [4] a few { $wound }s
        [5] a few { $wound }s
        [6] many { $wound }s
        [7] many { $wound }s
        [8] many { $wound }s
       *[other] a ton of { $wound }s
    }.

wound-bruise-80 = [color=crimson]monumental bruise[/color]
wound-bruise-50 = [color=crimson]huge bruise[/color]
wound-bruise-30 = [color=red]large bruise[/color]
wound-bruise-20 = [color=red]moderate bruise[/color]
wound-bruise-10 = [color=orange]small bruise[/color]
wound-bruise-5 = [color=yellow]tiny bruise[/color]

wound-cut-massive-45 = [color=crimson]severely jagged, deep laceration[/color]
wound-cut-massive-35 = [color=crimson]jagged, deep laceration[/color]
wound-cut-massive-25 = [color=crimson]massive cut[/color]
wound-cut-massive-10 = [color=red]massive bloody scab[/color]
wound-cut-massive-0 = [color=orange]massive scar[/color]

wound-cut-severe-25 = [color=crimson]jagged laceration[/color]
wound-cut-severe-15 = [color=red]severe laceration[/color]
wound-cut-severe-10 = [color=red]healing laceration[/color]
wound-cut-severe-5 = [color=orange]bloody scab[/color]
wound-cut-severe-0 = [color=yellow]scar[/color]

wound-cut-moderate-15 = [color=red]jagged cut[/color]
wound-cut-moderate-10 = [color=orange]moderate cut[/color]
wound-cut-moderate-5 = [color=yellow]cut[/color]
wound-cut-moderate-0 = [color=yellow]fading scar[/color]

wound-cut-small-7 = [color=orange]jagged small cut[/color]
wound-cut-small-3 = [color=yellow]small cut[/color]
wound-cut-small-0 = [color=yellow]fading scar[/color]

wound-puncture-massive-45 = [color=crimson]jagged, gaping hole[/color]
wound-puncture-massive-35 = [color=crimson]jagged, deep puncture[/color]
wound-puncture-massive-25 = [color=crimson]massive puncture[/color]
wound-puncture-massive-10 = [color=red]round bloody scab[/color]
wound-puncture-massive-0 = [color=orange]massive round scar[/color]

wound-puncture-severe-25 = [color=crimson]jagged puncture[/color]
wound-puncture-severe-15 = [color=red]severe puncture[/color]
wound-puncture-severe-10 = [color=red]healing puncture[/color]
wound-puncture-severe-5 = [color=orange]round bloody scab[/color]
wound-puncture-severe-0 = [color=yellow]round scar[/color]

wound-puncture-moderate-15 = [color=red]deep puncture wound[/color]
wound-puncture-moderate-10 = [color=orange]puncture wound[/color]
wound-puncture-moderate-5 = [color=yellow]round scab[/color]
wound-puncture-moderate-0 = [color=yellow]fading round scar[/color]

wound-puncture-small-7 = [color=orange]jagged small puncture[/color]
wound-puncture-small-3 = [color=yellow]small puncture[/color]
wound-puncture-small-0 = [color=yellow]fading round scar[/color]

wound-heat-carbonized-40 = [color=crimson]severely carbonized burn[/color]
wound-heat-carbonized-25 = [color=crimson]carbonized burn[/color]
wound-heat-carbonized-10 = [color=red]healing carbonized burn[/color]
wound-heat-carbonized-0 = [color=red]massive burn scar[/color]

wound-heat-severe-25 = [color=crimson]jagged, severe burn[/color]
wound-heat-severe-15 = [color=red]severe burn[/color]
wound-heat-severe-10 = [color=red]healing severe burn[/color]
wound-heat-severe-5 = [color=orange]healing burn[/color]
wound-heat-severe-0 = [color=orange]burn scar[/color]

wound-heat-moderate-15 = [color=red]severe burn[/color]
wound-heat-moderate-10 = [color=red]burn[/color]
wound-heat-moderate-5 = [color=orange]healing burn[/color]
wound-heat-moderate-0 = [color=orange]fading burn scar[/color]

wound-heat-small-7 = [color=orange]jagged small burn[/color]
wound-heat-small-3 = [color=orange]small burn[/color]
wound-heat-small-0 = [color=yellow]fading burn scar[/color]

wound-cold-petrified-45 = [color=lightblue]necrotic, petrified frostbite[/color]
wound-cold-petrified-35 = [color=lightblue]dark, petrified frostbite[/color]
wound-cold-petrified-25 = [color=lightblue]petrified frostbite[/color]
wound-cold-petrified-10 = [color=lightblue]flushed, thawing frostbite[/color]
wound-cold-petrified-0 = [color=lightblue]massive leathery scar[/color]

wound-cold-severe-25 = [color=lightblue]blistering, severe frostbite[/color]
wound-cold-severe-15 = [color=lightblue]severe frostbite[/color]
wound-cold-severe-10 = [color=lightblue]healing frostbite[/color]
wound-cold-severe-5 = [color=lightblue]thawing frostbite[/color]
wound-cold-severe-0 = [color=lightblue]blistered scar[/color]

wound-cold-moderate-15 = [color=lightblue]blistering, moderate frostbite[/color]
wound-cold-moderate-10 = [color=lightblue]moderate frostbite[/color]
wound-cold-moderate-5 = [color=lightblue]thawing frostbite[/color]
wound-cold-moderate-0 = [color=lightblue]fading red patch[/color]

wound-cold-small-7 = [color=lightblue]reddened frostnip[/color]
wound-cold-small-3 = [color=lightblue]frostnip[/color]
wound-cold-small-0 = [color=lightblue]cold patch[/color]

wound-caustic-sloughing-45 = [color=yellowgreen]necrotic, melting caustic burn[/color]
wound-caustic-sloughing-35 = [color=yellowgreen]blistering, melting caustic burn[/color]
wound-caustic-sloughing-25 = [color=yellowgreen]sloughing caustic burn[/color]
wound-caustic-sloughing-10 = [color=yellowgreen]shedding caustic burn[/color]
wound-caustic-sloughing-0 = [color=yellowgreen]massive blistered scar[/color]

wound-caustic-severe-25 = [color=yellowgreen]blistering, severe caustic burn[/color]
wound-caustic-severe-15 = [color=yellowgreen]severe caustic burn[/color]
wound-caustic-severe-10 = [color=yellowgreen]healing caustic burn[/color]
wound-caustic-severe-5 = [color=yellowgreen]bleached, inflamed skin[/color]
wound-caustic-severe-0 = [color=yellowgreen]blistered scar[/color]

wound-caustic-moderate-15 = [color=yellowgreen]blistering, moderate caustic burn[/color]
wound-caustic-moderate-10 = [color=yellowgreen]moderate caustic burn[/color]
wound-caustic-moderate-5 = [color=yellowgreen]inflamed skin[/color]
wound-caustic-moderate-0 = [color=yellowgreen]irritated scar[/color]

wound-caustic-small-7 = [color=yellowgreen]blistered caustic burn[/color]
wound-caustic-small-3 = [color=yellowgreen]small caustic burn[/color]
wound-caustic-small-0 = [color=yellowgreen]discolored burn[/color]

wound-shock-exploded-45 = [color=lightgoldenrodyellow]carbonized, exploded shock burn[/color]
wound-shock-exploded-35 = [color=lightgoldenrodyellow]charred, exploded shock burn[/color]
wound-shock-exploded-25 = [color=lightgoldenrodyellow]exploded shock burn[/color]
wound-shock-exploded-10 = [color=lightgoldenrodyellow]healing shock burn[/color]
wound-shock-exploded-0 = [color=lightgoldenrodyellow]massive electric scar[/color]

wound-shock-severe-25 = [color=lightgoldenrodyellow]charring, severe shock burn[/color]
wound-shock-severe-15 = [color=lightgoldenrodyellow]severe shock burn[/color]
wound-shock-severe-10 = [color=lightgoldenrodyellow]healing shock burn[/color]
wound-shock-severe-5 = [color=lightgoldenrodyellow]fading shock burn[/color]
wound-shock-severe-0 = [color=lightgoldenrodyellow]electric scar[/color]

wound-shock-moderate-15 = [color=lightgoldenrodyellow]mildly charred shock burn[/color]
wound-shock-moderate-10 = [color=lightgoldenrodyellow]moderate shock burn[/color]
wound-shock-moderate-5 = [color=lightgoldenrodyellow]fading shock burn[/color]
wound-shock-moderate-0 = [color=lightgoldenrodyellow]small blister[/color]

wound-shock-small-7 = [color=lightgoldenrodyellow]shock burn[/color]
wound-shock-small-3 = [color=lightgoldenrodyellow]small shock burn[/color]
wound-shock-small-0 = [color=lightgoldenrodyellow]fading shock burn[/color]
