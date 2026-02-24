const API_URL = "https://riftroulette-production.up.railway.app/api";
let isLoginMode = true;
let currentUser = JSON.parse(localStorage.getItem('user')) || null;
let currentLobbyCode = null;
let lobbyInterval = null; // Controla la auto-actualización de la sala

// --- NAVEGACIÓN SPA ---
function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => {
        v.style.display = 'none';
        v.classList.remove('active');
    });
    const target = document.getElementById(viewId);
    if (target) {
        target.style.display = 'block';
        target.classList.add('active');
    }
    
    document.getElementById('main-nav').style.display = viewId === 'view-auth' ? 'none' : 'flex';

    if (viewId === 'view-inventory') loadInventory();

    // Detener auto-actualización si salimos de la sala
    if (viewId !== 'view-lobby' && lobbyInterval) {
        clearInterval(lobbyInterval);
        lobbyInterval = null;
    }
}

// --- AUTH (LOGIN / REGISTRO) ---
function toggleAuthMode() {
    isLoginMode = !isLoginMode;
    document.getElementById('auth-title').innerText = isLoginMode ? "Bienvenido" : "Crear Cuenta";
    document.getElementById('auth-switch').innerText = isLoginMode ? "¿No tienes cuenta? Regístrate" : "¿Ya tienes cuenta? Ingresa";
}

async function handleAuth() {
    const user = document.getElementById('auth-user').value;
    const pass = document.getElementById('auth-pass').value;
    const endpoint = isLoginMode ? "Auth/login" : "Auth/register";

    try {
        const response = await fetch(`${API_URL}/${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Username: user, Password: pass })
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('user', JSON.stringify(data));
            currentUser = data;
            showView('view-home');
        } else {
            alert("Error en las credenciales o el usuario ya existe.");
        }
    } catch (e) {
        alert("Error de conexión con el servidor.");
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

// --- INVENTARIO (MIS SKINS) ---
async function loadInventory() {
    if (!currentUser) return;
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        if (!response.ok) throw new Error("Error obteniendo skins");
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        console.error("Error cargando inventario:", error);
        document.getElementById('themes-container').innerHTML = "<p>Error de conexión al cargar skins.</p>";
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
                ${grouped[tema].map(s => {
                    const champName = s.nombre.split(' ')[0];
                    const skinIndex = s.id.slice(-2);
                    const imgUrl = `https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${champName}_${parseInt(skinIndex)}.jpg`;

                    return `
                        <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin('${s.id}', this)">
                            <div class="skin-img-wrapper">
                                <img src="${imgUrl}" onerror="this.src='https://via.placeholder.com/300x170/121214/FFFFFF?text=${s.nombre}'">
                            </div>
                            <div class="skin-name">${s.nombre}</div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `).join('');
}

// ESTA ES LA FUNCIÓN CRÍTICA QUE GUARDA EL CLICK
async function toggleSkin(skinId, element) {
    if (!currentUser) return;
    
    // Evitar que el usuario haga doble click rápido y rompa la sincronización
    element.style.pointerEvents = 'none'; 
    const isNowOwned = !element.classList.contains('owned');

    try {
        const res = await fetch(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                userId: currentUser.userId,
                skinId: skinId,
                owned: isNowOwned
            })
        });

        if (res.ok) {
            // Si el servidor confirma, cambiamos el color visualmente
            element.classList.toggle('owned');
        } else {
            console.error("Error del servidor al guardar.");
            alert("No se pudo guardar la skin. Verifica tu conexión.");
        }
    } catch (error) {
        console.error("Error de fetch guardando skin:", error);
    } finally {
        element.style.pointerEvents = 'auto';
    }
}

// --- TEAM BUILDER Y SALAS ---
async function createNewLobby() {
    try {
        const response = await fetch(`${API_URL}/Lobby/create`, { method: 'POST' });
        const data = await response.json();
        await joinLobbyRequest(data.lobbyCode);
    } catch (e) { alert("Error al crear la sala"); }
}

async function joinLobbyRequest(code) {
    if (!currentUser) return;
    try {
        const res = await fetch(`${API_URL}/Lobby/join/${code}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ UserId: currentUser.userId, Username: currentUser.username })
        });

        if (res.ok) {
            currentLobbyCode = code;
            document.getElementById('display-code').innerText = `#${currentLobbyCode}`;
            showView('view-lobby');
            refreshTeamBuilder();
            
            // MAGIA: Auto-Actualizar la sala cada 3 segundos
            if (!lobbyInterval) {
                lobbyInterval = setInterval(refreshTeamBuilder, 3000);
            }
        } else {
            alert("La sala está llena o no existe.");
        }
    } catch (e) { console.error("Error uniéndose a sala:", e); }
}

function copyInviteLink() {
    const url = `${window.location.origin}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(url);
    const note = document.getElementById('notification');
    if (note) {
        note.classList.add('show');
        setTimeout(() => note.classList.remove('show'), 2500);
    }
}

async function refreshTeamBuilder() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetch(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}`);
        if (!res.ok) throw new Error("Error obteniendo datos del servidor");
        const data = await res.json();
        renderTeamBuilder(data);
    } catch (e) { console.error(e); }
}

function renderTeamBuilder(data) {
    const container = document.getElementById('team-results');
    if (!container) return;

    if (Object.keys(data).length === 0) {
        container.innerHTML = "<p style='text-align:center;'>Aún no hay coincidencias. Selecciona skins en tu inventario o invita amigos.</p>";
        return;
    }

    let html = "";
    const ordenLineas = ["Top", "Jungle", "Mid", "ADC", "Support"];

    for (const [tematica, lineas] of Object.entries(data)) {
        html += `<div class="team-group">
                    <h4>${tematica}</h4>
                    <div class="roles-grid">`;

        ordenLineas.forEach(rol => {
            html += `<div class="role-column">
                        <div class="role-title">${rol}</div>`;

            if (lineas[rol] && lineas[rol].length > 0) {
                lineas[rol].forEach(opcion => {
                    html += `<div class="role-option">
                                <strong>${opcion.campeon}</strong>
                                <span class="skin-name-small">${opcion.skin}</span>
                                <span class="player-name">👤 ${opcion.jugador}</span>
                             </div>`;
                });
            } else {
                html += `<div class="role-empty">Sin opciones</div>`;
            }
            html += `</div>`;
        });
        html += `</div></div>`;
    }
    container.innerHTML = html;
}

// --- INICIALIZACIÓN AL CARGAR LA PÁGINA ---
document.addEventListener('DOMContentLoaded', () => {
    if (!currentUser) {
        showView('view-auth');
    } else {
        showView('view-home');
    }

    const params = new URLSearchParams(window.location.search);
    const joinCode = params.get('join');
    if (joinCode && currentUser) {
        joinLobbyRequest(joinCode);
    }
});