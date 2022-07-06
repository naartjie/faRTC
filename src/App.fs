namespace DistroWeb

open Fable.Core
open Browser
open Browser.Types
open MessageStream
open WebRTC.Message

module App =
    [<Emit("console.log('DEBUG ' + $0, $1); window[$0] = $1")>]
    let private setGlobal (name: string) (value: obj) : unit = jsNative

    let signalling = new Stream<WebRTC.Message.Signalling>()
    let messages = new Stream<string>()
    let agent = WebRTC.init signalling messages

    setGlobal "agent" agent

    messages.Subscribe(fun msg -> console.log ("GOT A MESSAGE: ", msg))

    let websocket = WebSocket.Create("ws://localhost:8002")

    promise {
        do!
            Promise.create (fun resolve reject ->
                websocket.onopen <- fun _ -> resolve ()
                websocket.onerror <- fun err -> reject (exn (err.ToString())))

        signalling.Subscribe (fun msg ->
            match msg with
            | IceCandidate (candidate) -> websocket.send msg
            | Offer (offer) -> websocket.send msg
            | Answer (answer) -> (* TODO: ignore, really? *) ())
    }
    |> ignore

    websocket.onmessage <-
        fun msg ->
            let msg = msg.data.ToString() |> decode

            match msg with
            | None -> ()
            | Some msg ->
                match msg with
                | IceCandidate candidate ->
                    (candidate :?> RTCIceCandidateInit)
                    |> agent.addIceCandidate
                    |> ignore
                | Offer offer -> offer |> agent.setRemoteDescription |> ignore
                | Answer answer -> WebRTC.answer agent answer |> ignore
