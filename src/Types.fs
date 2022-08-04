module Types

type Peer = { uid: string }
type Connected = { me: Peer; others: list<Peer> }

type Model =
    | Initializing
    | Connected of Connected

type Msg =
    | Connect of Peer
    | PeerAdded of Peer
    | PeerRemoved of Peer
