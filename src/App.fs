module App

// open Browser
open Elmish
open Lit
open Lit.Elmish
open Types

let private hmr = HMR.createToken ()

Timer.register ()

let init () = Initializing, Cmd.none

let update msg model =
    (match model with
     | Initializing ->
         match msg with
         | Connect me -> Connected { me = me; others = [] }
         | _ -> failwith "invalid state change"
     | Connected info ->
         match msg with
         | PeerAdded peer -> Connected { info with others = info.others |> List.append [ peer ] }
         | PeerRemoved peer -> Connected { info with others = info.others |> List.except [ peer ] }
         | _ -> failwith "invalid state change"),
    Cmd.none

[<LitElement("app-root")>]
let App () =
    Hook.useHmr (hmr)

    let _ =
        LitElement.init (fun config -> config.useShadowDom <- false)

    let model, dispatch = Hook.useElmish (init, update)

    Signaling.register dispatch

    // let _timeout =
    //     Browser.Dom.window.setTimeout ((fun () -> dispatch (Connect { uid = "foo" })), 2000)

    match model with
    | Initializing -> html $"""establishing connection"""
    | Connected info ->
        html
            $"""
              <div><small><code>own uid:</code></small>{info.me.uid}</div>
              <div>connected to {info.others.Length} other peer(s)</div>
              {info.others
               |> List.map (fun o -> o.uid)
               |> String.concat " "}
            """
