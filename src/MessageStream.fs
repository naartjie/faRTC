module MessageStream

open Browser

open Fable.Core

[<Emit("$1.splice($0, 1)")>]
let private removeAt (idx: int) (arr: array<'a>) : array<'a> = jsNative

let private deque (arr: array<'a>) : 'a = (removeAt 0 arr).[0]

type Stream<'T>() =
    member val private subs = [||]
    member val private msgs = [||]

    member this.Send(msg: 'T) =
        if this.subs.Length = 0 then
            this.msgs.[this.msgs.Length] <- msg
        else
            this.subs |> Array.iter (fun fn -> fn msg)

    member this.Subscribe(fn: 'T -> unit) =
        this.subs.[this.subs.Length] <- fn

        while this.msgs.Length > 0 do
            this.msgs |> deque |> this.Send
