namespace DistroWeb

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types

module App =
    [<Emit("console.log('DEBUG ' + $0, $1); window[$0] = $1")>]
    let private setGlobal (name: string) (value: obj) : unit = jsNative

    let connectButton =
        document.getElementById "connect-button" :?> HTMLButtonElement

    let remoteDescriptionInput =
        document.getElementById "remote-description-input" :?> HTMLInputElement

    let candidateInput =
        document.getElementById "ice-candidate-input" :?> HTMLInputElement

    let candidates =
        document.getElementById "candidates" :?> HTMLDivElement

    let answerInput =
        document.getElementById "answer-input" :?> HTMLInputElement

    let answerButton =
        document.getElementById "answer-button" :?> HTMLButtonElement

    let handleMsg =
        fun (msg: string) -> console.log ("GOT A MESSAGE: ", msg)

    let iceCandidatesStream = new MessageStream.Stream()
    let offerStream = new MessageStream.Stream()

    let handleIceCandidate =
        fun (ic: RTCIceCandidate) -> iceCandidatesStream.Send(ic.candidate.ToString())

    let handleOffer =
        fun (offer: obj) -> offerStream.Send(offer |> JS.JSON.stringify)

    let agent =
        WebRTC.init handleMsg handleIceCandidate handleOffer

    setGlobal "agent" agent

    let websocket = WebSocket.Create("ws://localhost:8002")

    promise {
        do!
            Promise.create (fun resolve reject ->
                websocket.onopen <- fun _ -> resolve ()
                websocket.onerror <- fun err -> reject (exn (err.ToString())))

        iceCandidatesStream.Subscribe (fun x ->
            {| ``type`` = "candidate"
               candidate = x.ToString() |}
            |> JS.JSON.stringify
            |> websocket.send)

        offerStream.Subscribe(fun offer -> websocket.send offer)
    }
    |> ignore

    websocket.onmessage <-
        fun msg ->
            let msgStr = msg.data :?> string
            let msg = JS.JSON.parse msgStr
            let type_ = msg?``type``
            console.log ("ws got message", msg, msg?``type``)

            match type_ with
            | "candidate" ->
                let (candidate: string) = msg?candidate

                console.log ("makeRTCIceCandidate from...", candidate)

                candidate
                |> WebRTC.makeRTCIceCandidate
                |> agent.addIceCandidate
                |> ignore
            | "offer" ->
                console.log ("makeRTCSessionDescription from...", msgStr)

                msgStr
                |> WebRTC.makeRTCSessionDescription
                |> agent.setRemoteDescription
                |> ignore

            | "answer" -> msgStr |> WebRTC.sendAnswer agent |> ignore

            | _ -> ()


// answerButton.onclick <-
//     fun _ ->
//         promise {
//             let answer = answerInput.value
//             do! WebRTC.sendAnswer agent answer
//         }

// connectButton.onclick <-
//     fun _ ->
//         promise {
//             do!
//                 remoteDescriptionInput.value
//                 |> WebRTC.makeRTCSessionDescription
//                 |> agent.setRemoteDescription

//             do!
//                 candidateInput.value
//                 |> WebRTC.makeRTCIceCandidate
//                 |> agent.addIceCandidate

//             let! answer = agent.createAnswer ()
//             do! agent.setLocalDescription answer

//             candidates.innerHTML <- $"{candidates.innerHTML}<p/>{answer |> JS.JSON.stringify}"
//         }
//         |> ignore
