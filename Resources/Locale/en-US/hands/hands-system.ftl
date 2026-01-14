# Examine text after when they're holding something (in-hand)
comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } holding { $items }.
comp-hands-examine-empty = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } not holding anything.
comp-hands-examine-wrapper = { INDEFINITE($item) } [color=paleturquoise]{$itemName}[/color]
# imp edit ^ for plural stacks

hands-system-blocked-by = Blocked by
