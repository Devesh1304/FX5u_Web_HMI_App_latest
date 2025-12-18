"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/plcHub")
    .withAutomaticReconnect()
    .build();

// LISTEN FOR DATA
connection.on("ReceivePlcData", function (data) {
    // 'data' is the Dictionary we sent from C#
    for (const [key, value] of Object.entries(data)) {
        const el = document.getElementById(key);
        if (el) {
            // Do NOT update input if user is actively typing in it
            if (el.tagName === "INPUT" && document.activeElement === el) continue;

            let displayVal = value;
            // Auto-format Position fields (divide by 440)
            if (key.startsWith("Position")) {
                displayVal = (value / 440.0).toFixed(1);
            }

            if (el.tagName === "INPUT") el.value = displayVal;
            else el.textContent = displayVal;
        }
    }
});

// START FUNCTION
async function startSignalR(pageName) {
    try {
        await connection.start();
        console.log("Connected to SignalR. Joining group: " + pageName);
        await connection.invoke("JoinPage", pageName);
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        setTimeout(() => startSignalR(pageName), 3000); // Retry
    }
}