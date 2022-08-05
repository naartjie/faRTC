WTF?!?

Title: Debugging web apps

DO: [start the timer]

Hello!

I've been toying around with this idea of a distributed system in the browser
Using WebRTC for peer to peer communication

While busy with this app I learned about debugging in the browser
which is something I've always avoided and just opted for console.log instead

I'm using F#, which is a functional language which looks a bit like OCaml

I'm compiling it to JavaScript, so the first step to getting debugging is to turn on sourcemap generation

DO: [show package.json]

Sourcemaps are nice. If you have a compile step, you'll be able to click on the exact line of a log or an exception stacktrace

DO:
- [set theme]
- [click on log statement in chrome]
- [click on log statement in vscode]


1. debugging
   - using F#
   - generate sourcemaps
   - debug directly from chrome
   - debug from vscode
2. use the tech / syntax which makes sense for your mental model
   - literal html/css templates
     - jsx gets close, but it's not html
   - F#: you don't have to use js for the web