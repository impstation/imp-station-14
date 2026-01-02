plant-holder-crop-name = {$getsArticle ->
                                [true] { CAPITALIZE(INDEFINITE($seedName)) } [color=green]{ $seedName }[/color]
                                *[false] [color=green]{ CAPITALIZE($seedName) }[/color]
                        }
