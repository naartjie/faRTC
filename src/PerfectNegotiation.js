// @ts-check

/**
 * Perfect Negotiation pattern described on MDN:
 *
 * https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API/Perfect_negotiation
 */
function setupPerfectNegotiation(pc, signaling, polite) {
  let makingOffer = false;

  pc.onnegotiationneeded = async () => {
    try {
      makingOffer = true;
      await pc.setLocalDescription();
      signaling.Send({ type: "signaling::offer", data: pc.localDescription });
    } catch (err) {
      console.error("making offer", err);
    } finally {
      makingOffer = false;
    }
  };

  pc.onicecandidate = ({ candidate }) => {
    if (candidate !== null) {
      signaling.Send({ type: "signaling::candidate", data: candidate });
    }
  };

  pc.oniceconnectionstatechange = () => {
    if (pc.iceConnectionState === "failed") {
      pc.restartIce();
    }
  };

  let ignoreOffer = false;

  signaling.Subscribe(async (payload) => {
    try {
      switch (payload.type) {
        case "signaling::offer":
          const offerCollision = makingOffer || pc.signalingState != "stable";
          ignoreOffer = !polite && offerCollision;
          if (ignoreOffer) {
            return;
          }
          await pc.setRemoteDescription(payload.data);
          await pc.setLocalDescription();
          signaling.Send({
            type: "signaling::answer",
            data: pc.localDescription,
          });
          break;
        case "signaling::answer":
          await pc.setRemoteDescription(payload.data);
        case "signaling::candidate":
          try {
            await pc.addIceCandidate(payload.data);
          } catch (err) {
            if (!ignoreOffer) {
              throw err;
            }
          }
          break;
      }
    } catch (err) {
      console.error("processing signaling", err);
    }
  });
}

export default setupPerfectNegotiation;
