module MessageStream

open Fable.Core

[<Emit("$1.splice($0, 1)")>]
let private removeAt (idx: int) (arr: array<'a>) : array<'a> = jsNative

let private deque (arr: array<'a>) : 'a = (removeAt 0 arr).[0]

type InputStream<'T> =
    abstract member Send : 'T -> unit

type OutputStream<'T> =
    abstract member Subscribe : ('T -> unit) -> unit

/// Pipe is a uni-directional stream
type Pipe<'T>() =

    member val private subs = [||]
    member val private msgs = [||]

    interface InputStream<'T> with
        member this.Send(msg: 'T) =
            if this.subs.Length = 0 then
                this.msgs.[this.msgs.Length] <- msg
            else
                this.subs |> Array.iter (fun fn -> fn msg)

    interface OutputStream<'T> with
        member this.Subscribe(fn: 'T -> unit) =
            this.subs.[this.subs.Length] <- fn

            while this.msgs.Length > 0 do
                this.msgs
                |> deque
                |> (this :> InputStream<'T>).Send

[<AttachMembers>] // so we can use it in JS: https://fable.io/docs/communicate/fable-from-js.html#custom-behaviour
type DuplexStream<'T>(input: InputStream<'T>, output: OutputStream<'T>) =
    member __.Send(msg: 'T) = input.Send(msg)
    member __.Subscribe(fn: 'T -> unit) = output.Subscribe(fn)

let makeDuplexStream<'T> () =
    let pipeLeft, pipeRight = Pipe<'T>(), Pipe<'T>()
    let right = DuplexStream(pipeLeft, pipeRight)
    let left = DuplexStream(pipeRight, pipeLeft)
    left, right
