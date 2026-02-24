/**
 * CONFIGURACIÓN Y ESTADO GLOBAL
 */
const API_URL = "https://riftroulette-production.up.railway.app/api";
let isLoginMode = true;
let currentLobbyCode = "";
// Intentamos cargar el usuario del localStorage, si no, lo dejamos como null para forzar login
let currentUser = JSON.parse(localStorage.getItem('user')) || null;

/**
 * SISTEMA DE NAVEGACIÓN (SPA)
 */
function showView(viewId) {
    // 1. Ocultamos todas las secciones
    document.querySelectorAll('.view').forEach(v => {
        v.classList.remove('active');
        v.style.display = 'none';
    });

    // 2. Mostramos la sección solicitada
    const targetView = document.getElementById(viewId);
    if (targetView) {
        targetView.classList.add('active');
        targetView.style.display = 'block';
    }

    // 3. Control del Navbar: Solo visible si el usuario está logueado y no está en la vista de Auth
    const mainNav = document.getElementById('main-nav');
    if (mainNav) {
        mainNav.style.display = (viewId === 'view-auth' || !currentUser) ? 'none' : 'flex';
    }

    // 4. Carga automática de datos según la vista
    if (viewId === 'view-inventory' && currentUser) {
        loadInventory();
    }
}

/**
 * GESTIÓN DE AUTENTICACIÓN
 */
function toggleAuthMode() {
    isLoginMode = !isLoginMode;
    document.getElementById('auth-title').innerText = isLoginMode ? "Bienvenido" : "Crear Cuenta";
    document.getElementById('auth-switch').innerText = isLoginMode ? "¿No tienes cuenta? Regístrate" : "¿Ya tienes cuenta? Ingresa";
}

async function handleAuth() {
    const user = document.getElementById('auth-user').value;
    const pass = document.getElementById('auth-pass').value;

    if (!user || !pass) {
        alert("Por favor completa todos los campos");
        return;
    }

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
            const errorMsg = await response.text();
            alert("Error: " + (errorMsg || "Credenciales inválidas"));
        }
    } catch (e) {
        console.error("Error de Auth:", e);
        alert("Error de conexión con el servidor de Railway.");
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

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
        if (container) container.innerHTML = "<p>Error al cargar skins. Revisa la consola.</p>";
    }
}

function renderInventory(skins) {
    const container = document.getElementById('themes-container');
    if (!container) return;

    // Agrupamos por tema
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
                    // Generación de URL correcta: ChampionName_SkinIndex.jpg
                    const champName = s.nombre.split(' ')[0]; 
                    const skinIndex = s.id.slice(-2); // Los últimos 2 dígitos suelen ser el índice
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
async function createNewLobby() {
    try {
        const response = await fetch(`${API_URL}/Lobby/create`, { 
            method: 'POST',
            headers: { 
                'Authorization': `Bearer ${currentUser.token}`,
                'Content-Type': 'application/json'
            }
        });
        const data = await response.json();
        currentLobbyCode = data.lobbyCode;
        document.getElementById('display-code').innerText = `#${currentLobbyCode}`;
        showView('view-lobby');
    } catch (e) {
        alert("Error al crear la sala");
    }
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
/**
 * INICIALIZACIÓN AL CARGAR
 */
document.addEventListener('DOMContentLoaded', () => {
    // Si no hay usuario, mandamos a la vista de Auth
    if (!currentUser) {
        showView('view-auth');
    } else {
        // Si ya hay usuario, verificamos si viene de un link de invitación
        const params = new URLSearchParams(window.location.search);
        const joinCode = params.get('join');
        
        if (joinCode) {
            currentLobbyCode = joinCode;
            document.getElementById('display-code').innerText = `#${joinCode}`;
            showView('view-lobby');
        } else {
            showView('view-home');
        }
    }
    // --- LÓGICA DEL TEAM BUILDER Y SALAS ---
let currentLobbyCode = null;

// Crear sala y unirse automáticamente
async function createNewLobby() {
    try {
        const response = await fetch(`${API_URL}/Lobby/create`, { method: 'POST' });
        const data = await response.json();
        await joinLobbyRequest(data.lobbyCode);
    } catch (e) { alert("Error al crear la sala"); }
}

// Unirse a una sala
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

// Traer las composiciones del backend
async function refreshTeamBuilder() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetch(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}`);
        if (!res.ok) throw new Error("Error obteniendo datos");
        const data = await res.json();
        renderTeamBuilder(data);
    } catch (e) { console.error(e); }
}

// Dibujar las composiciones en pantalla
function renderTeamBuilder(data) {
    const container = document.getElementById('team-results');
    if (!container) return;

    if (Object.keys(data).length === 0) {
        container.innerHTML = "<p style='text-align:center;'>Aún no hay coincidencias. Invita a más amigos o agreguen más skins a su inventario.</p>";
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

// Auto-unirse si el link tiene un código ?join=XYZ
document.addEventListener('DOMContentLoaded', () => {
    const params = new URLSearchParams(window.location.search);
    const joinCode = params.get('join');
    if (joinCode && currentUser) {
        joinLobbyRequest(joinCode);
    }
});
});