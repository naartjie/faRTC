module Types

type Peer = { uid: string }
type Connected = { me: Peer; others: list<Peer> }

type Tab =
    | Connection
    | State
    | Vote

type ConnectionInfo =
    | Initializing
    | Connected of Connected

type Model =
    { ActiveTab: Tab
      Connection: ConnectionInfo }

type Msg =
    | Connect of Peer
    | PeerAdded of Peer
    | PeerRemoved of Peer
    | SelectTab of Tab
