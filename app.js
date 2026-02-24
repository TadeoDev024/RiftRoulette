// Configuración de la URL de producción (Railway)
const API_URL = "https://riftroulette-production.up.railway.app/api";

// Variables de estado global
let currentLobbyCode = "";
let currentUser = JSON.parse(localStorage.getItem('user')) || null;

/**
 * SISTEMA DE NAVEGACIÓN Y AUTH
 */
function showView(viewName) {
    document.querySelectorAll('.view').forEach(v => v.style.display = 'none');
    const target = document.getElementById(`view-${viewName}`);
    if (target) target.style.display = 'block';
}

async function login() {
    const username = document.getElementById('user').value;
    // En un sistema real enviarías también el password
    try {
        const response = await fetch(`${API_URL}/Auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username: username, password: "password_placeholder" })
        });

        if (response.ok) {
            const data = await response.json();
            currentUser = data;
            localStorage.setItem('user', JSON.stringify(data));
            showView('inventory');
            loadInventory();
        } else {
            alert("Error en el inicio de sesión");
        }
    } catch (error) {
        console.error("Error de conexión:", error);
    }
}

/**
 * GESTIÓN DE INVENTARIO
 */
async function loadInventory() {
    if (!currentUser) return;
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        console.error("Error cargando inventario:", error);
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
            <h3>${tema}</h3>
            <div class="skins-row">
                ${grouped[tema].map(s => `
                    <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin(${s.id}, this)">
                        <img src="https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${s.nombre.replace(/ /g, '')}_0.jpg" onerror="this.src='https://via.placeholder.com/150'">
                        <span>${s.nombre}</span>
                    </div>
                `).join('')}
            </div>
        </div>
    `).join('');
}

async function toggleSkin(skinId, element) {
    const isOwned = !element.classList.contains('owned');
    element.classList.toggle('owned');

    try {
        await fetch(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                userId: currentUser.userId, 
                skinId: skinId, 
                owned: isOwned 
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
    // Corrección: window.location.origin (en minúscula)
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
        // Petición a la URL de producción de Railway
        const response = await fetch(`${API_URL}/roulette/spin/${currentLobbyCode}`);
        if (!response.ok) throw new Error("No hay combinaciones válidas");
        
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

// Inicialización
document.addEventListener('DOMContentLoaded', () => {
    const params = new URLSearchParams(window.location.search);
    const joinCode = params.get('join');
    if (joinCode) {
        currentLobbyCode = joinCode;
        showView('lobby');
        // Aquí podrías disparar la función para unirse automáticamente
    }
});