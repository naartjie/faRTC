module Toast

open Lit

let private hmr = HMR.createToken ()

let register () = ()

let mutable messages: list<string> = []

let mutable requestUpdate = fun () -> ()

let addMessage msg =
    messages <- messages |> List.append [ msg ]
// requestUpdate ()

[<LitElement("toast-it")>]
let toastIt () =
    Hook.useHmr (hmr)

    let _host, _props =
        LitElement.init (fun cfg -> cfg.useShadowDom <- false)

    requestUpdate <- (fun () -> _host.requestUpdate ())

    let alert msg =
        html
            $"""
            <div class="alert alert-success shadow-lg">
              <div>
                <button class="btn btn-square btn-outline btn-xs ml-auto">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                  </svg>
                </button>
                <span>{msg}</span>
              </div>
            </div>
            """

    html
        $"""
        <div class="toast toast-end">
          {messages |> List.map alert}
        </div>
        """
