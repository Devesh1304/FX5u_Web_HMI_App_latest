"use strict";

document.addEventListener('DOMContentLoaded', () => {

    // --- 1. ALARM RULE BOOK & MODAL SETUP (Now safely inside DOMContentLoaded) ---
    const alarmConfig = {
        1: { title: "SERVO FAULT", message: "Servo Motor is in fault Condition." },
        2: { title: "TORQUE LIMIT", message: "torque Limit has been reached." },
        7: { title: "EMERGENCY", message: "Emergency button has been pressed." },
        8: { title: "BARCODE", message: "Scan barcode mismatched." },
    };

    const alarmModal = document.getElementById('alarmModal');
    const modalTitle = document.getElementById('modalTitle');
    const modalMessage = document.getElementById('modalMessage');
   // const alarmCodeValue = document.getElementById('alarmCodeValue');
    const modalAcknowledgeBtn = document.getElementById('modalAcknowledgeBtn');
    const modalCloseBtn = alarmModal ? alarmModal.querySelector('.close-button') : null;

    const hideAlarmPopup = () => {
        if (alarmModal) {
            alarmModal.style.display = 'none';
        }
    };

    //if (modalAcknowledgeBtn) modalAcknowledgeBtn.onclick = hideAlarmPopup;
    if (modalAcknowledgeBtn) {
        modalAcknowledgeBtn.addEventListener('click', async () => {
            try {
                // 1. Send the command to the C# backend to reset the PLC register
                const response = await fetch('/Index?handler=AcknowledgeAlarm', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

                if (!response.ok) {
                    throw new Error("Server responded with an error.");
                }

                // 2. On success, hide the popup immediately
                hideAlarmPopup();
                console.log("Alarm acknowledged successfully.");

            } catch (error) {
                console.error("Failed to acknowledge alarm:", error);
                // Optionally, display an error message to the user here
            }
        });
    }
    if (modalCloseBtn) modalCloseBtn.onclick = hideAlarmPopup;


    // --- 2. SIGNALR CONNECTION SETUP ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/plcHub")
        .withAutomaticReconnect()
        .build();


    // --- 3. SIGNALR MESSAGE HANDLERS ---
    connection.on("NavigateToPage", (url) => {
        if (window.location.pathname.toLowerCase() !== url.toLowerCase()) {
            window.location.href = url;
        }
    });

    connection.on("UpdateAlarmState", (alarmCode) => {
        if (alarmModal && alarmCode > 0 && alarmConfig[alarmCode]) {
            const config = alarmConfig[alarmCode];
            modalTitle.textContent = config.title;
            modalMessage.textContent = config.message;
          //  alarmCodeValue.textContent = alarmCode;
            alarmModal.style.display = 'flex';
        } else {
            hideAlarmPopup();
        }
    });

    // --- 4. CONNECTION LOGIC ---
    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };

    connection.onclose(async () => {
        await start();
    });

    start();
});