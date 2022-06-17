"use strict";

const express = require("express");
const WebSocket = require("ws");
const http = require("http");

const app = express();
const { PORT: port = "8002" } = process.env;
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const sendTo = (connection, message) => {
  connection.send(JSON.stringify(message));
};

const broadcastExcept = (connections, connection, msg) => {
  connections
    .filter((c) => c !== connection)
    .forEach((conn) => {
      console.log(`sendTo(...)`);
      sendTo(conn, msg);
    });
};

let connections = [];

let offers = [];
let candidates = [];

wss.on("connection", (ws) => {
  connections.push(ws);
  console.log(`got new connection: connections=${connections.length}`);

  ws.on("close", () => {
    console.log(`connection closed`);
    let idx = connections.indexOf(ws);
    if (idx > -1) {
      connections.splice(idx, 1);
    }
  });

  ws.on("message", (msg) => {
    let data;
    try {
      data = JSON.parse(msg);
    } catch (e) {
      console.error("Invalid JSON: " + msg.toString("utf-8"));
      return;
    }

    const { type } = data;
    switch (type) {
      case "offer":
        offers.push(data);
        broadcastExcept(connections, ws, data);
        break;
      case "answer":
        broadcastExcept(connections, ws, data);
        break;
      case "candidate":
        candidates.push(data);
        broadcastExcept(connections, ws, data);
        break;

      default:
        console.error("Invalid message type, dropping message");
        break;
    }
  });

  sendTo(ws, { type: "hello" });
  offers.forEach((offer) => sendTo(ws, offer));
});

server.listen(port, () =>
  console.log(`Signaling Server running on port: ${port}`)
);
