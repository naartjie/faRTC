module MessageStream

open Fable.Core

[<Emit("$1.splice($0, 1)")>]
let private removeAt (idx: int) (arr: array<'a>) : 'a = jsNative

let private deque (arr: array<'a>) : 'a = removeAt 0 arr

type Stream() =
    member val private subs = [||]
    member val private msgs = [||]

    member this.Send(msg: string) =
        // printfn $"Stream.Send(%s{msg})"
        if this.subs.Length = 0 then
            this.msgs.[this.msgs.Length] <- msg
        else
            this.subs |> Array.iter (fun fn -> fn msg)

    member this.Subscribe(fn: string -> unit) =
        // printfn $"subscribing %A{this.msgs}"
        this.subs.[this.subs.Length] <- fn

        while this.msgs.Length > 0 do
            this.msgs |> deque |> this.Send
