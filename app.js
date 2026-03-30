const API_URL = "https://skinsynergy-api.onrender.com/api";
let currentUser = JSON.parse(localStorage.getItem('user')) || null;
let currentLobbyCode = null;
let lobbyInterval = null;

// NAVEGACIÓN ENTRE VISTAS
function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    const target = document.getElementById(viewId);
    if (target) target.classList.add('active');

    const nav = document.getElementById('main-nav');
    nav.style.display = (viewId === 'view-auth' || !currentUser) ? 'none' : 'flex';

    if (viewId === 'view-inventory') loadInventory();
    
    // Si salimos de la sala, dejamos de pedir actualizaciones
    if (viewId !== 'view-lobby' && lobbyInterval) {
        clearInterval(lobbyInterval);
        lobbyInterval = null;
        currentLobbyCode = null;
    }
}

// AUTENTICACIÓN
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

    if (!user || !pass) return alert("Completa los campos");

    try {
        const res = await fetch(`${API_URL}/Rift/${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Username: user, Password: pass })
        });

        if (res.ok) {
            const data = await res.json();
            currentUser = { userId: data.userId, username: user };
            localStorage.setItem('user', JSON.stringify(currentUser));
            showView('view-home');
        } else {
            const errorData = await res.json().catch(() => ({ message: "Error desconocido" }));
            alert(errorData.message || "Error de credenciales");
        }
    } catch (e) {
        console.error(e);
        alert("El servidor está despertando. Por favor, intenta de nuevo en 20 segundos.");
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

// INVENTARIO Y BUSCADOR
async function loadInventory() {
    const container = document.getElementById('themes-container');
    container.innerHTML = "<p>Cargando colección...</p>";
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        container.innerHTML = "<p>Error al conectar con la base de datos.</p>";
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
        const hasVisible = group.querySelectorAll('.skin-card[style="display: block;"]').length > 0;
        group.style.display = hasVisible ? "block" : "none";
    });
}

async function toggleSkin(skinId, element) {
    const isNowOwned = !element.classList.contains('owned');
    try {
        const res = await fetch(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: currentUser.userId, skinId: skinId, owned: isNowOwned })
        });
        if (res.ok) element.classList.toggle('owned');
    } catch (e) { console.error(e); }
}

// SALAS Y LOBBY
async function createNewLobby() {
    try {
        const res = await fetch(`${API_URL}/Lobby/create`, { method: 'POST' });
        const data = await res.json();
        joinLobbyRequest(data.lobbyCode);
    } catch (e) { alert("Error al crear sala"); }
}

// NUEVA FUNCIÓN: Unirse desde la caja de texto
async function joinLobbyFromInput() {
    const input = document.getElementById('lobby-code-input');
    if (!input) return;
    
    const code = input.value.trim().toUpperCase();
    
    if (!code) {
        alert("Por favor, ingresa un código de sala válido.");
        return;
    }
    await joinLobbyRequest(code);
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
            document.getElementById('display-code').innerText = `#${code}`;
            showView('view-lobby');
            refreshTeamBuilder();
            if (lobbyInterval) clearInterval(lobbyInterval);
            lobbyInterval = setInterval(refreshTeamBuilder, 3000); // Actualiza cada 3 segundos
        } else {
            const data = await res.json();
            alert(data.message || "Sala inexistente o llena.");
        }
    } catch (e) { console.error(e); }
}

// NUEVA FUNCIÓN: Copiar el código al portapapeles
function copyInviteLink() {
    if (!currentLobbyCode) return;
    // Crea una URL para que el amigo entre directo: tusitio.com/?join=A4X9P2
    const inviteUrl = `${window.location.origin}${window.location.pathname}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(inviteUrl).then(() => {
        alert(`¡Enlace copiado! Envíalo a tus amigos:\n${inviteUrl}`);
    }).catch(() => {
        alert(`Comparte este código con tus amigos: ${currentLobbyCode}`);
    });
}

async function refreshTeamBuilder() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetch(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}`);
        const data = await res.json();
        renderTeamBuilder(data);
    } catch (e) { console.error(e); }
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

// INICIALIZACIÓN
document.addEventListener('DOMContentLoaded', () => {
    if (!currentUser) showView('view-auth');
    else {
        // Magia: Si el amigo entró con el link (?join=CODIGO), lo mete directo a la sala
        const joinCode = new URLSearchParams(window.location.search).get('join');
        joinCode ? joinLobbyRequest(joinCode) : showView('view-home');
    }
});