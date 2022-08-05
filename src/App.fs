module App

open Elmish
open Lit
open Lit.Elmish
open Types
open Browser

let private hmr = HMR.createToken ()

Timer.register ()
Theme.register ()
Toast.register ()

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
     | RtcMsg str, _ ->
         Toast.addMessage str
         model
     | _ -> failwith "invalid state change"),
    Cmd.none

let connectionInfo2 (connected: Connected) =

    let header =
        html
            $"""
            <tr>
                <th></th>
                <th>Name</th>
                <th>Connected</th>
                <th>Duration</th>
            </tr>
            """

    let rows =
        connected.others
        |> List.append [ { connected.me with uid = $"{connected.me.uid} (me 👻)" } ]
        |> Lit.mapiUnique
            (fun pc -> pc.uid)
            (fun i pc ->
                html
                    $"""
                    <tr>
                        <th>{i + 1}</th>
                        <td>{pc.uid}</td>
                        <td>14h17m02s</td>
                        <td>1m 22s</td>
                    </tr>
                    """)

    html
        $"""
        <div>{connected.others.Length + 1} nodes</div>
        <div class="overflow-x-auto">
            <table class="table table-compact w-full">
                <thead>
                    {header}
                </thead>
                <tbody>
                    {rows}
                </tbody>
                <tfoot>
                    {header}
                </tfoot>
            </table>
        </div>
        """

let connectionInfo =
    function
    | Initializing -> html $"""establishing connection"""
    | Connected info -> connectionInfo2 info

let renderTabs model dispatch =

    let tabs =
        html
            $"""
                <div class="tabs">
                    <a @click={Ev(fun _ -> dispatch (SelectTab Vote))} class="tab tab-bordered {if model.ActiveTab = Vote then
                                                                                                    "tab-active"
                                                                                                else
                                                                                                    ""}">Timer</a>
                    <a @click={Ev(fun _ -> dispatch (SelectTab Connection))} class="tab tab-bordered {if model.ActiveTab = Connection then
                                                                                                          "tab-active"
                                                                                                      else
                                                                                                          ""}">Nodes</a>
                    <a @click={Ev(fun _ -> dispatch (SelectTab State))} class="tab tab-bordered {if model.ActiveTab = State then
                                                                                                     "tab-active"
                                                                                                 else
                                                                                                     ""}">State</a>
                </div>
                """

    let content =
        match model.ActiveTab with
        | Connection -> connectionInfo model.Connection
        | State -> html $""" <div>TODO: render state view</div> """
        | Vote -> html $""" <countdown-timer duration="300" update-title> </countdown-timer> """

    html
        $"""
        {tabs}
        {content}
        """

let view model dispatch =
    html
        $"""
        <div class="p-16 max-w-5xl">
            <h2>Distributed Webs Machine</h2>
            {renderTabs model dispatch}
        <div>
        """
// <toast-it></toast-it>


[<LitElement("app-root")>]
let app () =
    Hook.useHmr (hmr)

    let _ =
        LitElement.init (fun config -> config.useShadowDom <- false)

    let model, dispatch = Hook.useElmish (init, update)

    Signaling.register dispatch

    view model dispatch
