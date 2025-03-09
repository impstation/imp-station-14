## PROSPECTOR ROUND, ANTAG & GAMEMODE TEXT

prospector-announcement-sender = ???

prospector-title = Prospector
prospector-description = A rag-tag band of scavengers descend upon the station.

roles-antag-prospector-name = Prospector
roles-antag-prospector-description = Snatch and grab whatever isn't nailed down - be it critical station assets or entire chunks of the station.

prospector-gamemode-title = Prospectors
prospector-gamemode-description =
    Scanners have detected that this pocket of space ain't big enough for the both of us.

## ROUNDEND TEXT

prospector-roundend-prospector-count = {$initialCount ->
    [1] There was {$initialCount} [color=#dc402a]Prospector[/color].
    *[other] There were {$initialCount} [color=#dc402a]Prospectors[/color].
}
prospector-roundend-entropy-count = The Prospector Gang snatched {$count} spesos worth of scrap.

prospector-roundend-prospmajor = [color=#dc402a]Prospector gang major victory![/color]
prospector-roundend-prospminor = [color=#dc402a]Prospector gang minor victory![/color]
prospector-roundend-neutral = [color=yellow]Neutral outcome![/color]
prospector-roundend-crewminor = [color=green]Crew minor victory![/color]
prospector-roundend-crewmajor = [color=green]Crew major victory![/color]

prospector-summary-prospmajor = The Prospector Gang's antics incurred massive financial losses for the station.
prospector-summary-prospminor = The Prospector Gang's antics have caused a dent in the station's finances.
prospector-summary-neutral = The crew and the Prospector Gang are more or less unaffected and nothing significant was taken.
prospector-summary-crewminor = The Prospector Gang lost some good folk in the struggle and didn't get to nab much.
prospector-summary-crewmajor = The Prospector Gang is no more...

prospector-elimination-shuttle-call = Based on scans from our long-range sensors, that gaggle of thieves has been scattered to the wind. Mosey on to the emergency shuttle we're calling over. ETA: {$time} {$units}. You can recall the shuttle to extend the shift.
prospector-elimination-announcement = Based on scans from our long-range sensors, that gaggle of thieves has been scattered to the wind. Shuttle is already called.


## BRIEFINGS

prospector-role-roundstart-fluff =
    You're a rough and tumble prospector who rolled on up to the station with your plucky crew
    for stuff to bring to the Folks Back Home at your planetary settlement.
    Your folks have given you a shopping list of stuff to 'reclaim' from
    the chumps working at this here Nanotransen station. Get after it.

prospector-role-short-briefing =
    You are a Prospector!
    Your objectives are listed in the character menu.
    Read more about your role in the guidebook entry.

## UI / BASE POPUP

prospector-ui-roundstart-title = Prospector

prospector-ui-popup-confirm = Confirm



## OBJECTIVES / CHARACTERMENU

objective-issuer-prospector = [bold][color=#dc402a]The Folks Back Home[/color][/bold]

objective-prospector-charactermenu = Snatch and grab everything you can from the station.

objective-condition-lootcash-title = Loot the Station
objective-condition-lootcash-desc = Pilfer at least {$count} worth of spesos from the station.
objective-condition-prospectorobj-title = Snatch us a {$itemname}
objective-condition-prospectorobj-desc = The Folks Back Home need you to snatch 'em up a {$itemname}. Keep it near the Reclamation Beacon. 
objective-condition-prospectorobjmult-title = Steal {$count} {MAKEPLURAL($itemName)}
objective-condition-prospectorobjmult-desc = The Folks Back Home need {$count} {MAKEPLURAL($itemName)} to keep things running. Keep 'em near the Reclamation Beacon. 