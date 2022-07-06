module WebRTC

open Fable.Core
open Browser
open Browser.Types
open MessageStream

JsInterop.importAll "webrtc-adapter" |> ignore

[<Emit("{ offerToReceiveAudio: 1 }")>]
let private offerToReceiveAudioOptions () : RTCOfferOptions = jsNative

[<Emit("new RTCIceCandidate($0)")>]
let makeRTCIceCandidate (x: obj) : RTCIceCandidate = jsNative

[<Emit("new RTCSessionDescription($0)")>]
let makeRTCSessionDescription (x: obj) : RTCSessionDescriptionInit = jsNative

[<Emit("$0.setLocalDescription()")>]
let newSchoolSetLocalDescription (agent: RTCPeerConnection) : JS.Promise<RTCSessionDescriptionInit> = jsNative

module Message =
    type Signalling =
        | IceCandidate of RTCIceCandidate
        | Offer of RTCSessionDescriptionInit
        | Answer of RTCSessionDescriptionInit

    type private Enc = { ``type``: string; data: obj }

    let encode (msg: Signalling) =
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

let private create () =
    let conf =
        [| RTCIceServer.Create([| "stun:stun3.l.google.com:19302" |]) |]
        |> RTCConfiguration.Create
        |> fun x ->
            // x.iceTransportPolicy <- Some RTCIceTransportPolicy.Relay
            x.iceTransportPolicy <- Some RTCIceTransportPolicy.All
            x

    RTCPeerConnection.Create(conf)

let private createChannel (agent: RTCPeerConnection) name = agent.createDataChannel name

let init (signalling: Stream<Message.Signalling>) (messages: Stream<string>) =
    let agent = create ()
    let _chann = createChannel agent "default"

    agent.onicecandidate <-
        fun ev ->
            match ev.candidate with
            | None -> ()
            | Some candidate ->
                candidate
                |> Message.Signalling.IceCandidate
                |> signalling.Send

    agent.ondatachannel <-
        fun ev ->
            ev.channel.onmessage <-
                fun msg ->
                    let txt = msg.data.ToString()
                    messages.Send(txt)

    promise {
        let! offer = newSchoolSetLocalDescription agent

        offer
        |> Message.Signalling.Offer
        |> signalling.Send
    }
    |> ignore

    agent

let answer (agent: RTCPeerConnection) (answer: RTCSessionDescriptionInit) =
    promise { do! agent.setRemoteDescription answer }
