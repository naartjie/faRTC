module App

open Elmish
open Lit
open Lit.Elmish
open Types

let private hmr = HMR.createToken ()

Timer.register ()

let init () =
    { ActiveTab = Connection
      Connection = Initializing },
    Cmd.none

let update msg model =
    (match msg, model.Connection with
     | (Connect me, Initializing) -> { model with Connection = Connected { me = me; others = [] } }
     | (PeerAdded peer, Connected info) ->
         { model with Connection = Connected { info with others = info.others |> List.append [ peer ] } }
     | (PeerRemoved peer, Connected info) ->
         { model with Connection = Connected { info with others = info.others |> List.except [ peer ] } }
     | (SelectTab tab, _) -> { model with ActiveTab = tab }
     | _ -> failwith "invalid state change"),
    Cmd.none

let connectionInfo =
    function
    | Initializing -> html $"""establishing connection"""
    | Connected info ->
        html
            $"""
              <div><small><code>I am:</code></small> {info.me.uid}</div>
              <div>I am connected to {info.others.Length} other peer(s)</div>
              {info.others
               |> Lit.mapUnique (fun o -> o.uid) (fun o -> html $"<div> → {o.uid}</div>")}
            """

let renderTab model =
    match model.ActiveTab with
    | Connection -> connectionInfo model.Connection
    | State -> html $""" <div>render state</div> """
    | Vote -> html $""" <div>render vote</div> """

[<LitElement("app-root")>]
let app () =
    Hook.useHmr (hmr)

    let _ =
        LitElement.init (fun config -> config.useShadowDom <- false)

    let model, dispatch = Hook.useElmish (init, update)

    Signaling.register dispatch

    let tabActive = "tab-active"
    let noStyle = ""

    html
        $"""
        <h2>Distributed Webs Machine</h2>
        <div class="tabs">
            <a @click={Ev(fun _ -> dispatch (SelectTab Connection))} class="tab tab-bordered {if model.ActiveTab = Connection then
                                                                                                  tabActive
                                                                                              else
                                                                                                  noStyle}">Nodes</a>
            <a @click={Ev(fun _ -> dispatch (SelectTab State))} class="tab tab-bordered {if model.ActiveTab = State then
                                                                                             tabActive
                                                                                         else
                                                                                             noStyle}">State</a>
            <a @click={Ev(fun _ -> dispatch (SelectTab Vote))} class="tab tab-bordered {if model.ActiveTab = Vote then
                                                                                            tabActive
                                                                                        else
                                                                                            noStyle}">Vote</a>
        </div>
        {renderTab model}
        """
