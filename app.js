let currentLobbyCode = "";

// Función para copiar invitación
function copyInviteLink() {
    const url = `${window.location.origin}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(url);
    
    const note = document.getElementById('notification');
    note.classList.add('show');
    setTimeout(() => note.classList.remove('show'), 2500);
}

// Lógica de animación de Ruleta
async function triggerRoulette() {
    const btn = document.getElementById('spin-btn');
    const wheel = document.getElementById('wheel');
    const resultDiv = document.getElementById('result-display');

    btn.disabled = true;
    wheel.style.transform = `rotate(${3600 + Math.random() * 360}deg)`;

    // Llamada al algoritmo del Backend de la Fase 2
    try {
        const response = await fetch(`/api/roulette/spin/${currentLobbyCode}`);
        const data = await response.json();

        setTimeout(() => {
            wheel.innerText = data.tematica;
            wheel.style.transform = "rotate(0deg)";
            wheel.style.transition = "none";
            
            // Mostrar asignaciones (quién juega qué)
            resultDiv.innerHTML = data.assignments.map(a => `
                <div class="assign-card">
                    <strong>${a.username}</strong>: ${a.skinName}
                </div>
            `).join('');
            
            btn.disabled = false;
        }, 4000);
    } catch (e) {
        alert("No se encontró temática válida para este grupo.");
        btn.disabled = false;
    }
}