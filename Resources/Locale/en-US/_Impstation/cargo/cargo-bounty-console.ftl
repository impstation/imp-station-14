bounty-console-claimed-by-none = None
bounty-console-claimed-by-unknown = Unknown
bounty-console-claimed-by = Claimed by: {$claimant}
bounty-console-status-label = Status: {$status ->
        [2] [color=limegreen]On Shuttle[/color]
        [1] Waiting
        *[other] [color=orange]Undelivered[/color]
    }
bounty-console-status = {$status ->
        [2] On Shuttle
        [1] Waiting
        *[other] Undelivered
    }

bounty-console-status-tooltip = {$status ->
    [2] This bounty is on the shuttle, ready to be delivered to the trade station
    [1] This bounty is waiting to be fulfilled
    *[other] This bounty has not yet been sent out for fulfilment
    }
