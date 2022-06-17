module WebRTC

open Fable.Core
open Browser
open Browser.Types

let private adapter: obj = JsInterop.importAll "webrtc-adapter"

[<Emit("{ offerToReceiveAudio: 1 }")>]
let private offerToReceiveAudioOptions () : RTCOfferOptions = jsNative

[<Emit("new RTCIceCandidate(JSON.parse($0))")>]
let makeRTCIceCandidate (x: string) : RTCIceCandidateInit = jsNative

[<Emit("new RTCSessionDescription(JSON.parse($0))")>]
let makeRTCSessionDescription (x: string) : RTCSessionDescriptionInit = jsNative

// [<Emit("new RTCSessionDescription(JSON.parse($0))")>]
// let makeRTCSessionDescription (x: string) : RTCSessionDescriptionInit = jsNative

module Messaging =
    type Scp = { Scp: string }

    type Signalling =
        | IceCandidate of RTCIceCandidate
        | Offer of RTCSessionDescriptionInit
        | Answer of RTCSessionDescriptionInit

    let encode () = "TODO"
    let decode (msg: string) = IceCandidate (* TODOOO *)

let private create () =
    let conf =
        // [| RTCIceServer.Create([| "turn:serverAddress" |], "account", "pass", RTCIceCredentialType.Password) |]
        // [| RTCIceServer.Create([| "stun:stun.l.google.com:19302" |]) |]
        [| RTCIceServer.Create([| "stun:stun3.l.google.com:19302" |]) |]
        |> RTCConfiguration.Create
        |> fun x ->
            // x.iceTransportPolicy <- Some RTCIceTransportPolicy.Relay
            x.iceTransportPolicy <- Some RTCIceTransportPolicy.All
            x

    RTCPeerConnection.Create(conf)

let private createChannel (agent: RTCPeerConnection) name = agent.createDataChannel name

let init (handleMsg: string -> unit) (handleIceCandidate: RTCIceCandidate -> unit) (handleOffer: obj -> unit) =
    let agent = create ()
    let _chann = createChannel agent "default"

    agent.onicecandidate <-
        fun ev ->
            match ev.candidate with
            | Some candidate ->
                // console.log ("TODO got ice candidate", candidate |> JS.JSON.stringify)
                handleIceCandidate candidate
            | None -> ()

    agent.ondatachannel <-
        fun ev ->
            console.log "new channel created, adding onmessage event handler"
            let chann = ev.channel
            chann.onmessage <- fun msg -> handleMsg (msg.data.ToString())

    promise {
        // TODO
        // let! offer = agent.setLocalDescription ()
        let! offer = agent.createOffer ()
        do! agent.setLocalDescription offer
        handleOffer offer
    }
    |> ignore

    agent

let sendAnswer (agent: RTCPeerConnection) (answer: string) =
    promise {
        do!
            answer
            |> makeRTCSessionDescription
            |> agent.setRemoteDescription
    }
