# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.

-health-analyzer-rating = { $rating ->
    [good] ([color=#00D3B8]good[/color])
    [okay] ([color=#30CC19]okay[/color])
    [poor] ([color=#bdcc00]poor[/color])
    [bad] ([color=#E8CB2D]bad[/color])
    [awful] ([color=#EF973C]awful[/color])
    [dangerous] ([color=#FF6C7F]dangerous[/color])
   *[other] (unknown)
    }

health-analyzer-window-entity-brain-health-text = Brain Activity:
health-analyzer-window-entity-blood-pressure-text = Blood Pressure:
health-analyzer-window-entity-blood-oxygenation-text = Blood Saturation:
health-analyzer-window-entity-blood-circulation-text = Blood Circulation:
health-analyzer-window-entity-heart-rate-text = Heart Rate:
health-analyzer-window-entity-heart-health-text = Heart Health:

health-analyzer-window-entity-brain-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-heart-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-heart-rate-value = {$value}bpm { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-oxygenation-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-pressure-value = {$diastolic}/{$systolic} { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-circulation-value = {$value}% { -health-analyzer-rating(rating: $rating) }

wound-internal-fracture = [color=red]Patient has internal fractures.[/color]
wound-incision = [color=red]Patient has open incision.[/color]
wound-clamped = [color=red]Patient has clamped arteries.[/color]
wound-retracted = [color=red]Patient has retracted skin.[/color]
wound-ribcage-open = [color=red]Patient has open ribcage.[/color]
wound-arterial-bleeding = [color=red]Patient has arterial bleeding.[/color]
