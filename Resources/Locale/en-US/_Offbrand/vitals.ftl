-crew-monitor-vitals-rating = { $rating ->
    [good] [color=limegreen]{$text}[/color]
    [okay] [color=yellowgreen]{$text}[/color]
    [poor] [color=yellow]{$text}[/color]
    [bad] [color=orange]{$text}[/color]
    [awful] [color=red]{$text}[/color]
    [dangerous] [color=crimson]{$text}[/color]
   *[other] unknown
    }

offbrand-crew-monitoring-heart-rate = { -crew-monitor-vitals-rating(text: $rate, rating: $rating) }bpm
offbrand-crew-monitoring-blood-pressure = { -crew-monitor-vitals-rating(text: $systolic, rating: $rating) }/{ -crew-monitor-vitals-rating(text: $diastolic, rating: $rating) }
offbrand-crew-monitoring-oxygenation = { -crew-monitor-vitals-rating(text: $oxygenation, rating: $rating) }% air
