module Timer

open Lit
open Browser
open System

let private hmr = HMR.createToken ()

let register () = ()

module Icons =
    let replay =
        html
            $"""<svg xmlns="http://www.w3.org/2000/svg" enable-background="new 0 0 24 24" height="24px" viewBox="0 0 24 24" width="24px" fill="#000000"><title>Reset</title><g><rect fill="none" height="24" width="24"/><rect fill="none" height="24" width="24"/><rect fill="none" height="24" width="24"/></g><g><g/><path d="M12,5V1L7,6l5,5V7c3.31,0,6,2.69,6,6s-2.69,6-6,6s-6-2.69-6-6H4c0,4.42,3.58,8,8,8s8-3.58,8-8S16.42,5,12,5z"/></g></svg>"""

    let pause =
        html
            $"""<svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 0 24 24" width="24px" fill="#000000"><title>Pause</title><path d="M0 0h24v24H0V0z" fill="none"/><path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"/></svg>"""

    let play =
        html
            $"""<svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 0 24 24" width="24px" fill="#000000"><title>Start</title><path d="M0 0h24v24H0V0z" fill="none"/><path d="M10 8.64L15.27 12 10 15.36V8.64M8 5v14l11-7L8 5z"/></svg>"""

    let noIcon =
        html
            $"""<svg xmlns="http://www.w3.org/2000/svg" enable-background="new 0 0 24 24" height="24px" viewBox="0 0 24 24" width="24px" fill="#000000"></svg>"""

[<LitElement("countdown-timer")>]
let countdownTimer () =
    Hook.useHmr (hmr)

    let _, props =
        LitElement.init (fun cfg ->
            cfg.useShadowDom <- false

            cfg.props <-
                {| duration = Prop.Of(60, attribute = "duration")
                   updateTitle = Prop.Of(false, attribute = "update-title") |})

    let running, setRunning = Hook.useState false

    let remaining, setRemaining =
        Hook.useState (1000 * props.duration.Value)

    let end_, setEnd =
        Hook.useState (DateTime.UtcNow.AddMilliseconds remaining)

    let start () =
        setRunning true
        setEnd (DateTime.UtcNow.AddMilliseconds remaining)

    let pause () = setRunning false

    let reset () =
        let finished = running && remaining <= 0

        let stillRunning =
            if finished then
                setRunning false
                false
            else
                running

        let newRemaining =
            (1000 * props.duration.Value)
            - if stillRunning then 1 else 0

        setRemaining newRemaining
        setEnd (DateTime.UtcNow.AddMilliseconds newRemaining)

    let timeStr showHundredths remaining =
        let min = remaining / 1000 / 60
        let sec = remaining / 1000 % 60

        $"""{min}m%02d{sec}s{if showHundredths then
                                 $"%02d{remaining / 10 % 100}"
                             else
                                 ""}"""

    let minSecHun remaining =
        let min = remaining / 1000 / 60
        let sec = remaining / 1000 % 60
        let hun = remaining / 10 % 100
        min, sec, hun

    Hook.useEffectOnChange (
        (running, end_),
        (fun (running, end_) ->
            let tick () =
                let now = DateTime.UtcNow
                (end_ - now).TotalMilliseconds |> int |> max 0

            let setTitle newRemaining =
                if props.updateTitle.Value then
                    let newTitle =
                        $"""{timeStr false newRemaining}{if running then "" else " ⏸"}"""

                    if document.title <> newTitle then
                        document.title <- newTitle

            let interval =
                if running then
                    window.setInterval (
                        (fun () ->
                            let newRemaining = tick ()
                            setRemaining newRemaining
                            setTitle newRemaining),
                        50
                    )
                else
                    setTitle remaining
                    -1

            { new IDisposable with
                member __.Dispose() = window.clearInterval interval })
    )

    let fn, icon =
        if running then
            (pause,
             if remaining > 0 then
                 Icons.pause
             else
                 Icons.noIcon)
        else
            (start,
             if remaining > 0 then
                 Icons.play
             else
                 Icons.noIcon)

    let (m, s, _) = minSecHun remaining

    html
        $"""
            <div class="grid grid-flow-col gap-5 text-center auto-cols-max justify-center">
                <div class="flex flex-col">
                    <span class="countdown font-mono text-5xl">
                    <span style="--value:{m};"></span>
                    </span>
                    min
                </div>
                <div class="flex flex-col">
                    <span class="countdown font-mono text-5xl">
                    <span style="--value:{s};"></span>
                    </span>
                    sec
                </div>
            </div>
            <div class="btn-group grid grid-flow-col text-center auto-cols-max justify-center">
                <button @click={Ev(fun _ -> fn ())} class="btn btn-sm btn-secondary">{icon}</button>
                <button @click={Ev(fun _ -> reset ())} class="btn btn-sm btn-secondary">{Icons.replay}</button>
            </div>
        """
