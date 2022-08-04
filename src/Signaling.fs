module Signaling

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open MessageStream
open Types

let mutable dispatch = fun _ -> ()
let register (dispatch': Types.Msg -> unit) = dispatch <- dispatch'

[<Emit("console.log('DEBUG ' + $0, $1); window[$0] = $1")>]
let private setGlobal (name: string) (value: obj) : unit = jsNative

[<Emit("$0.setRemoteDescription($1)")>]
let agent_setRemoteDescription (agent: obj) (description: obj) : JS.Promise<unit> = jsNative

[<Emit("$0.addIceCandidate($1)")>]
let agent_addIceCandidate (agent: obj) (iceCandidate: obj) : JS.Promise<unit> = jsNative

[<Emit("new URL($0, $1)")>]
let createUrl (relativePath: string) (basePath: string) : URL = jsNative

let websocket =
    let url = createUrl "/ws" window.location.href

    url.protocol <-
        match window.location.protocol with
        | "https:" -> "wss"
        | _ -> "ws"

    WebSocket.Create(url.ToString())

promise {
    do!
        Promise.create (fun resolve reject ->
            websocket.onopen <- fun _ -> resolve ()
            websocket.onerror <- fun err -> reject (exn (err.ToString())))

    console.log "websocket ready"
}
|> ignore

// TODO this is horrible
let mutable myUid = ""

let mutable peers =
    Map.empty<string, (DuplexStream<obj> * DuplexStream<string>)>

let setupPeerConnection polite =
    let pc = WebRTC.createPeerConnection ()

    let ch =
        let setupChan (ch: RTCDataChannel) (resolve: RTCDataChannel -> unit) =
            ch.onopen <- fun _ -> resolve ch
            ch.onclose <- fun _ -> console.error ("data channel closed")

        Promise.create (fun resolve reject ->
            if polite then
                pc.ondatachannel <- fun ev -> setupChan ev.channel resolve
            else
                setupChan (pc.createDataChannel "default") resolve)

    // "us" is the end we connect to RTCPeerConnection
    // "ws" is the end we connect to websocket
    let signalingUs, signalingWs = makeDuplexStream ()
    WebRTC.setupPerfectNegotiation pc signalingUs polite

    let dataPeerConnection, dataApp = makeDuplexStream<string> ()

    promise {
        // wait until the channel is ready
        let! ch = ch

        ch.addEventListener (
            "message",
            fun msg ->
                let txt: string = msg?data
                dataPeerConnection.Send(txt)
        )
    }
    |> ignore

    dataPeerConnection.Subscribe (fun msg ->
        promise {
            // wait until the channel is open before sending the message
            let! ch = ch
            ch.send (U4.Case1 msg)
        }
        |> ignore)

    signalingWs, dataApp

let addNewConnection myUid otherUid polite =
    let signaling, dataChan = setupPeerConnection polite

    peers <- peers.Add(otherUid, (signaling, dataChan))

    dataChan.Send("testing data, are we connected yet buddy?")
    dataChan.Subscribe(fun msg -> console.log ($"got msg from [%s{otherUid}]: %s{msg}"))

    signaling.Subscribe (fun msg ->
        msg?src <- myUid
        msg?dest <- otherUid
        websocket.send (msg |> JS.JSON.stringify))

let logLoud str =
    console.log ($"%%c%s{str}", "color: green; background: yellow; font-size: 30px")

websocket.onmessage <-
    fun msg ->
        let payload = msg.data.ToString() |> JS.JSON.parse

        match payload?``type`` with

        | "config::own_uid" as x ->
            myUid <- payload?data?uid
            // setGlobal "uid" (fun () -> logLoud myUid)
            Connect { uid = myUid } |> dispatch

            promise {
                do! Promise.sleep 400
                console.log $"uid={myUid}"
                logLoud myUid
            }
            |> ignore

        | "config::new_uid" as x ->
            let msg = payload?data
            let otherUid, polite = msg?otherUid, msg?polite
            addNewConnection myUid otherUid polite
            PeerAdded { uid = otherUid } |> dispatch

        | "config::all_uids" as x ->
            let (others: array<obj>) = payload?data

            others
            |> Array.iter (fun x ->
                let otherUid = x?otherUid
                let polite = x?polite
                addNewConnection myUid otherUid polite
                PeerAdded { uid = otherUid } |> dispatch)

        | "config::remove_uid" as x ->
            let uid = payload?data?uid
            peers <- peers.Remove(uid)
            PeerRemoved { uid = uid } |> dispatch

        | x when x.StartsWith("signaling::") ->
            let src = payload?src

            peers.TryFind(src)
            |> Option.iter (fun (signaling, _) -> signaling.Send(msg.data.ToString() |> JS.JSON.parse))

        | _ -> console.error ("ws UNMATCHED!!!", payload)
