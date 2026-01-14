cargo-invoice-text = [head=2]Order #{$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Requested by:[/bold]"} {$requester}

    {"[head=3]Item Contents[/head]"}
    {$contents}
    {"[head=3]Order Information[/head]"}
    {"[bold]Payer[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Approved by:[/bold]"} {$approver}
    {"[bold]Reason:[/bold]"} {$reason}
