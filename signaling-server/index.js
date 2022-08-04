"use strict";

const express = require("express");
const WebSocket = require("ws");
const http = require("http");
const { generateName } = require("./names");

const app = express();
const { PORT: port = "8002" } = process.env;
const server = http.createServer(app);
const path = "/ws";
const wss = new WebSocket.Server({ server, path });

const sendTo = (conn, message) => {
  console.log(`TO >==> [${conn.uid}]`, message);
  conn.send(JSON.stringify(message));
};

const broadcastExcept = (connections, conn, msg) => {
  Object.values(connections)
    .filter((c) => c.uid !== conn.uid)
    .forEach((conn) => {
      sendTo(conn, msg);
    });
};

let connections = {};

wss.on("connection", (conn) => {
  do {
    conn.uid = generateName();
  } while (connections[conn.uid]);
  connections[conn.uid] = conn;
  console.log(
    `new connection [${conn.uid}]: connections=${
      Object.keys(connections).length
    }`
  );

  sendTo(conn, { type: "config::own_uid", data: { uid: conn.uid } });
  sendTo(conn, {
    type: "config::all_uids",
    data: Object.values(connections)
      .filter((c) => c.uid !== conn.uid)
      .map((c) => ({
        otherUid: c.uid,
        polite: true,
      })),
  });
  broadcastExcept(connections, conn, {
    type: "config::new_uid",
    data: { otherUid: conn.uid, polite: false },
  });

  conn.on("close", () => {
    console.log(`connection closed [${conn.uid}]`);
    delete connections[conn.uid];
    broadcastExcept(
      connections,
      {},
      { type: "config::remove_uid", data: { uid: conn.uid } }
    );
  });

  conn.on("message", (msg) => {
    let payload;
    try {
      payload = JSON.parse(msg);
    } catch (e) {
      console.log({ msg });
      console.error("Invalid JSON: " + msg.toString("utf-8"));
      return;
    }

    console.log(`FROM <==< [${conn.uid}]`, payload);

    switch (payload.type) {
      case "signaling::candidate":
      case "signaling::offer":
      case "signaling::answer":
        let { dest } = payload;
        if (dest && connections[dest]) {
          sendTo(connections[dest], payload);
        } else {
          console.error("`dest` field not set or wrong value", payload);
        }
        break;
      default:
        console.error("Invalid message type, dropping message", payload);
        break;
    }
  });
});

server.listen(port, () =>
  console.log(`Signaling Server running on :${port}${path}`)
);
