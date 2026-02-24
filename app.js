// Configuración de la URL de producción (Railway)
const API_URL = "https://riftroulette-production.up.railway.app/api";

// Variables de estado global
let currentLobbyCode = "";
// Usuario de prueba si no hay uno logueado
let currentUser = JSON.parse(localStorage.getItem('user')) || { userId: 1, username: "admin" };

/**
 * GESTIÓN DE INVENTARIO
 */
async function loadInventory() {
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        if (!response.ok) throw new Error("Error en el servidor");
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        console.error("Error cargando inventario:", error);
        const container = document.getElementById('themes-container');
        if (container) container.innerHTML = "<p>Error al conectar con la API.</p>";
    }
}

function renderInventory(skins) {
    const container = document.getElementById('themes-container');
    if (!container) return;

    const grouped = skins.reduce((acc, skin) => {
        if (!acc[skin.tema]) acc[skin.tema] = [];
        acc[skin.tema].push(skin);
        return acc;
    }, {});

    container.innerHTML = Object.keys(grouped).map(tema => `
        <div class="theme-group">
            <h3 class="theme-title">${tema}</h3>
            <div class="skins-row">
                ${grouped[tema].map(s => `
                    <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin('${s.id}', this)">
                        <img src="https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${s.nombre.replace(/\s/g, '')}_0.jpg" 
                             onerror="this.src='https://via.placeholder.com/150x80?text=Skin'">
                        <span>${s.nombre}</span>
                    </div>
                `).join('')}
            </div>
        </div>
    `).join('');
}

async function toggleSkin(skinId, element) {
    const isNowOwned = !element.classList.contains('owned');
    element.classList.toggle('owned');

    try {
        await fetch(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                userId: currentUser.userId, 
                skinId: skinId, 
                owned: isNowOwned 
            })
        });
    } catch (error) {
        console.error("Error guardando skin:", error);
    }
}

/**
 * GESTIÓN DE LOBBY Y RULETA
 */
function copyInviteLink() {
    // Corrección: window.location (en minúsculas)
    const url = `${window.location.origin}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(url);
    
    const note = document.getElementById('notification');
    if (note) {
        note.classList.add('show');
        setTimeout(() => note.classList.remove('show'), 2500);
    }
}

async function triggerRoulette() {
    const btn = document.getElementById('spin-btn');
    const wheel = document.getElementById('wheel');
    const resultDiv = document.getElementById('result-display');

    if (!currentLobbyCode) {
        alert("Debes estar en una sala activa");
        return;
    }

    btn.disabled = true;
    wheel.style.transform = `rotate(${3600 + Math.random() * 360}deg)`;

    try {
        const response = await fetch(`${API_URL}/roulette/spin/${currentLobbyCode}`);
        if (!response.ok) throw new Error("No se encontró temática válida para este grupo.");
        
        const data = await response.json();

        setTimeout(() => {
            wheel.innerText = data.tematica;
            wheel.style.transform = "rotate(0deg)";
            wheel.style.transition = "none";
            
            resultDiv.innerHTML = data.assignments.map(a => `
                <div class="assign-card">
                    <strong>Usuario ID ${a.userId}</strong>: ${a.skinName}
                </div>
            `).join('');
            
            btn.disabled = false;
        }, 4000);
    } catch (e) {
        alert(e.message);
        btn.disabled = false;
        wheel.style.transform = "rotate(0deg)";
    }
}

/**
 * INICIALIZACIÓN
 */
document.addEventListener('DOMContentLoaded', () => {
    // Carga el inventario al iniciar
    loadInventory();

    // Verifica si se está uniendo a una sala por URL
    const params = new URLSearchParams(window.location.search);
    const joinCode = params.get('join');
    if (joinCode) {
        currentLobbyCode = joinCode;
        const displayCode = document.getElementById('display-code');
        if (displayCode) displayCode.innerText = `#${joinCode}`;
    }
});