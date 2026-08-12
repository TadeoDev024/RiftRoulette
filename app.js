const API_URL = "https://skinsynergy-api.onrender.com/api";
let currentUser = JSON.parse(localStorage.getItem('user')) || null;
let currentLobbyCode = null;
let lobbyInterval = null;
let currentSuggestion = null;

// ---------- HELPERS DE UI ----------
function showSpinner() { document.getElementById('global-spinner').style.display = 'block'; }
function hideSpinner() { document.getElementById('global-spinner').style.display = 'none'; }

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerText = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

// CSS para toasts (inyectado dinámicamente)
const toastStyle = document.createElement('style');
toastStyle.textContent = `
    .toast {
        position: fixed;
        bottom: 20px;
        right: 20px;
        background: #1a1a1c;
        color: #f0f0f0;
        padding: 12px 20px;
        border-radius: 8px;
        border-left: 4px solid #00cfd4;
        z-index: 10000;
        font-weight: 600;
        animation: slideIn 0.3s ease;
    }
    .toast-error { border-left-color: #ff4a4a; }
    @keyframes slideIn { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
`;
document.head.appendChild(toastStyle);

// ---------- FETCH CON AUTH ----------
function fetchAuth(url, options = {}) {
    const token = localStorage.getItem('token');
    options.headers = {
        'Content-Type': 'application/json',
        ...(options.headers || {}),
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };
    return fetch(url, options);
}

// ---------- NAVEGACIÓN ----------
function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    const target = document.getElementById(viewId);
    if (target) target.classList.add('active');

    const nav = document.getElementById('main-nav');
    nav.style.display = (viewId === 'view-auth' || !currentUser) ? 'none' : 'flex';

    if (viewId === 'view-inventory') loadInventory();
    
    if (viewId !== 'view-lobby') {
        if (lobbyInterval) {
            clearInterval(lobbyInterval);
            lobbyInterval = null;
            currentLobbyCode = null;
            currentSuggestion = null;
        }
        const currentUrl = new URL(window.location);
        if (currentUrl.searchParams.has('join')) {
            currentUrl.searchParams.delete('join');
            window.history.replaceState({}, document.title, currentUrl.toString());
        }
    }
}

// ---------- AUTENTICACIÓN ----------
function toggleAuthMode() {
    const title = document.getElementById('auth-title');
    const switchBtn = document.getElementById('auth-switch');
    const actionBtn = document.querySelector('#view-auth .btn-main');

    if (title.innerText === "Bienvenido") {
        title.innerText = "Registrarse";
        actionBtn.innerText = "Crear Cuenta";
        switchBtn.innerText = "¿Ya tienes cuenta? Ingresa";
    } else {
        title.innerText = "Bienvenido";
        actionBtn.innerText = "Ingresar";
        switchBtn.innerText = "¿No tienes cuenta? Regístrate";
    }
}

async function handleAuth() {
    const userField = document.getElementById('auth-user');
    const passField = document.getElementById('auth-pass');
    if (!userField || !passField) return;

    const user = userField.value.trim();
    const pass = passField.value.trim();
    const isLogin = document.getElementById('auth-title').innerText === "Bienvenido";
    const endpoint = isLogin ? "login" : "register";

    if (!user || !pass) return showToast("Completa los campos", "error");

    showSpinner();
    try {
        const res = await fetchAuth(`${API_URL}/Rift/${endpoint}`, {
            method: 'POST',
            body: JSON.stringify({ Username: user, Password: pass })
        });

        if (res.ok) {
            const data = await res.json();
            currentUser = { userId: data.userId, username: user };
            localStorage.setItem('user', JSON.stringify(currentUser));
            localStorage.setItem('token', data.token);
            showView('view-home');
            showToast("Sesión iniciada correctamente");
        } else {
            const errorData = await res.json().catch(() => ({ message: "Error desconocido" }));
            showToast(errorData.message || "Error de credenciales", "error");
        }
    } catch (e) {
        console.error(e);
        showToast("El servidor está despertando. Intenta de nuevo en 20 segundos.", "error");
    } finally {
        hideSpinner();
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

// ---------- INVENTARIO ----------
async function loadInventory() {
    const container = document.getElementById('themes-container');
    container.innerHTML = "<p>Cargando colección...</p>";
    showSpinner();
    try {
        const response = await fetchAuth(`${API_URL}/Rift/skins/${currentUser.userId}`);
        if (!response.ok) throw new Error("Token inválido o expirado");
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        container.innerHTML = "<p>Error al conectar con la base de datos. Intenta recargar.</p>";
        showToast("Error al cargar inventario", "error");
    } finally {
        hideSpinner();
    }
}

function renderInventory(skins) {
    const container = document.getElementById('themes-container');
    if (!container) return;

    const grouped = skins.reduce((acc, skin) => {
        const champ = skin.campeon || "Unknown";
        if (!acc[champ]) acc[champ] = [];
        acc[champ].push(skin);
        return acc;
    }, {});

    const sortedChamps = Object.keys(grouped).sort();

    container.innerHTML = sortedChamps.map(champ => `
        <div class="theme-group">
            <h3 class="theme-title">${champ}</h3>
            <div class="skins-row">
                ${grouped[champ].map(s => {
                    const champId = s.campeonId || "Unknown";
                    const skinIndex = parseInt(s.id) % 1000;
                    const imgUrl = `https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${champId}_${skinIndex}.jpg`;
                    return `
                        <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin('${s.id}', this)">
                            <div class="skin-img-wrapper">
                                <img src="${imgUrl}" onerror="this.src='https://via.placeholder.com/300x170?text=Error+Carga'">
                            </div>
                            <div class="skin-name">
                                ${s.nombre}<br>
                                <small>${s.tema}</small>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `).join('');
}

function filterSkins() {
    const query = document.getElementById('skin-search').value.toLowerCase();
    document.querySelectorAll('.skin-card').forEach(card => {
        card.style.display = card.innerText.toLowerCase().includes(query) ? "block" : "none";
    });
    document.querySelectorAll('.theme-group').forEach(group => {
        const hasVisible = group.querySelectorAll('.skin-card[style*="block"]').length > 0;
        group.style.display = hasVisible ? "block" : "none";
    });
}

async function toggleSkin(skinId, element) {
    const isNowOwned = !element.classList.contains('owned');
    try {
        const res = await fetchAuth(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            body: JSON.stringify({ userId: currentUser.userId, skinId: skinId, owned: isNowOwned })
        });
        if (res.ok) {
            element.classList.toggle('owned');
            showToast(isNowOwned ? "Skin añadida" : "Skin eliminada");
        } else {
            showToast("No se pudo actualizar", "error");
        }
    } catch (e) {
        console.error(e);
        showToast("Error de conexión", "error");
    }
}

// ---------- SALAS ----------
async function createNewLobby() {
    showSpinner();
    try {
        const res = await fetchAuth(`${API_URL}/Lobby/create`, { method: 'POST' });
        const data = await res.json();
        await joinLobbyRequest(data.lobbyCode);
        showToast("Sala creada");
    } catch (e) {
        showToast("Error al crear sala", "error");
    } finally {
        hideSpinner();
    }
}

async function joinLobbyFromInput() {
    const input = document.getElementById('lobby-code-input');
    if (!input) return;
    const code = input.value.trim().toUpperCase();
    if (!code) return showToast("Ingresa un código válido", "error");
    await joinLobbyRequest(code);
}

async function joinLobbyRequest(code) {
    if (!currentUser) return;
    showSpinner();
    try {
        const res = await fetchAuth(`${API_URL}/Lobby/join/${code}`, {
            method: 'POST',
            body: JSON.stringify({ UserId: currentUser.userId, Username: currentUser.username })
        });

        if (res.ok) {
            currentLobbyCode = code;
            document.getElementById('display-code').innerText = `#${code}`;
            showView('view-lobby');
            await refreshTeamBuilder();
            if (lobbyInterval) clearInterval(lobbyInterval);
            lobbyInterval = setInterval(refreshTeamBuilder, 5000); // actualiza cada 5s
            showToast("Unido a la sala");
        } else {
            const data = await res.json();
            showToast(data.message || "Sala inexistente o llena", "error");
        }
    } catch (e) {
        console.error(e);
        showToast("Error de conexión", "error");
    } finally {
        hideSpinner();
    }
}

function copyInviteLink() {
    if (!currentLobbyCode) return;
    const inviteUrl = `${window.location.origin}${window.location.pathname}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(inviteUrl).then(() => {
        showToast("Enlace copiado");
    }).catch(() => {
        showToast(`Código: ${currentLobbyCode}`);
    });
}

async function refreshTeamBuilder() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetchAuth(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}`);
        const data = await res.json();
        renderTeamBuilder(data);
        // También cargamos la sugerencia
        await loadSuggestion();
    } catch (e) {
        console.error(e);
    }
}

async function loadSuggestion() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetchAuth(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}/suggest`);
        const data = await res.json();
        currentSuggestion = data;
        renderSuggestion(data);
    } catch (e) {
        console.error(e);
    }
}

function renderSuggestion(data) {
    const suggestionContainer = document.getElementById('suggestion-container');
    if (!suggestionContainer) return;

    if (!data || data.length === 0) {
        suggestionContainer.innerHTML = "<p>No hay suficientes datos para sugerir una combinación.</p>";
        return;
    }

    let html = `<div class="suggestion-header">
        <h3>✨ Sugerencia Automática</h3>
        <button class="btn-secondary" onclick="loadSuggestion()">Regenerar</button>
    </div><div class="suggestion-grid">`;

    data.forEach(item => {
        html += `
            <div class="suggestion-card">
                <strong>${item.rol}</strong>
                <div>${item.campeon}</div>
                <div class="skin-name-small">${item.skin}</div>
                <div class="player-name">👤 ${item.jugador}</div>
            </div>`;
    });

    html += `</div>`;
    suggestionContainer.innerHTML = html;
}

function renderTeamBuilder(data) {
    const container = document.getElementById('team-results');
    if (Object.keys(data).length === 0) {
        container.innerHTML = "<p>Esperando jugadores o skins en común...</p>";
        return;
    }
    let html = "";
    for (const [tematica, lineas] of Object.entries(data)) {
        html += `<div class="team-group"><h4>${tematica}</h4><div class="roles-grid">`;
        ["Top", "Jungle", "Mid", "ADC", "Support"].forEach(rol => {
            html += `<div class="role-column"><div class="role-title">${rol}</div>`;
            if (lineas[rol]?.length > 0) {
                lineas[rol].forEach(op => {
                    html += `<div class="role-option"><strong>${op.campeon}</strong><br>${op.skin}<br><small>👤 ${op.jugador}</small></div>`;
                });
            } else html += `<div class="role-empty">-</div>`;
            html += `</div>`;
        });
        html += `</div></div>`;
    }
    container.innerHTML = html;
}

// ---------- INICIALIZACIÓN ----------
document.addEventListener('DOMContentLoaded', () => {
    if (!currentUser) showView('view-auth');
    else {
        const joinCode = new URLSearchParams(window.location.search).get('join');
        joinCode ? joinLobbyRequest(joinCode) : showView('view-home');
    }
});