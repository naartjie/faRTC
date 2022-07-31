module WebRTC

open Fable.Core
open Browser
open Browser.Types
open MessageStream

JsInterop.importAll "webrtc-adapter" |> ignore

[<Emit("new RTCIceCandidate($0)")>]
let makeRTCIceCandidate (x: obj) : RTCIceCandidate = jsNative

[<Emit("new RTCSessionDescription($0)")>]
let makeRTCSessionDescription (x: obj) : RTCSessionDescriptionInit = jsNative

[<Emit("$0.setLocalDescription()")>]
let newSchoolSetLocalDescription (agent: RTCPeerConnection) : JS.Promise<unit> = jsNative

[<Emit("$0")>]
let cast (x: RTCSessionDescription) : RTCSessionDescriptionInit = jsNative

type Signaler = { onmessage: obj; send: obj }

[<Import("default", from = "./PerfectNegotiation")>]
let setupPerfectNegotiation (pc: RTCPeerConnection) (signaling: DuplexStream<obj>) (polite: bool) : unit = jsNative

module Message =
    type Signaling =
        | IceCandidate of RTCIceCandidate
        | Offer of RTCSessionDescriptionInit
        | Answer of RTCSessionDescriptionInit

    type private Enc = { ``type``: string; data: obj }

    let encode (msg: Signaling) =
        match msg with
        | IceCandidate candidate ->
            { ``type`` = "candidate"
              data = candidate }
        | Offer offer -> { ``type`` = "offer"; data = offer }
        | Answer answer -> { ``type`` = "answer"; data = answer }
        |> JS.JSON.stringify

    let decode (msg: string) =
        let msg = JS.JSON.parse (msg) :?> Enc

        match msg.``type`` with
        | "candidate" ->
            msg.data
            |> makeRTCIceCandidate
            |> IceCandidate
            |> Some
        | "answer" ->
            msg.data
            |> makeRTCSessionDescription
            |> Answer
            |> Some
        | "offer" ->
            msg.data
            |> makeRTCSessionDescription
            |> Offer
            |> Some
        | "hello" -> None
        | _ -> failwith "unsupported message type"

let createPeerConnection () =
    let conf =
        [| RTCIceServer.Create([| "stun:stun3.l.google.com:19302" |]) |]
        |> RTCConfiguration.Create
        |> fun x ->
            // x.iceTransportPolicy <- Some RTCIceTransportPolicy.Relay
            x.iceTransportPolicy <- Some RTCIceTransportPolicy.All
            x

    RTCPeerConnection.Create(conf)

[<Emit("console.log('DEBUG ' + $0, $1); window[$0] = $1")>]
let private setGlobal (name: string) (value: obj) : unit = jsNative
