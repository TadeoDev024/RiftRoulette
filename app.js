const API_URL = "https://skinsynergy-api.onrender.com/api";
let isLoginMode = true;
let currentLobbyCode = null;
let lobbyInterval = null; 
let currentUser = JSON.parse(localStorage.getItem('user')) || null;

function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => {
        v.classList.remove('active');
        v.style.display = 'none';
    });

    const targetView = document.getElementById(viewId);
    if (targetView) {
        targetView.classList.add('active');
        targetView.style.display = 'block';
    }

    const mainNav = document.getElementById('main-nav');
    if (mainNav) {
        mainNav.style.display = (viewId === 'view-auth' || !currentUser) ? 'none' : 'flex';
    }

    if (viewId === 'view-inventory' && currentUser) loadInventory();

    if (viewId !== 'view-lobby' && lobbyInterval) {
        clearInterval(lobbyInterval);
        lobbyInterval = null;
    }
}

function toggleAuthMode() {
    isLoginMode = !isLoginMode;
    document.getElementById('auth-title').innerText = isLoginMode ? "Bienvenido" : "Crear Cuenta";
    document.getElementById('auth-switch').innerText = isLoginMode ? "¿No tienes cuenta? Regístrate" : "¿Ya tienes cuenta? Ingresa";
}

async function handleAuth() {
    const user = document.getElementById('auth-user').value;
    const pass = document.getElementById('auth-pass').value;
    if (!user || !pass) return alert("Completa todos los campos");

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
            alert("Credenciales inválidas o el usuario ya existe.");
        }
    } catch (e) {
        alert("Error de conexión con el servidor.");
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

async function loadInventory() {
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        if (!response.ok) throw new Error("Error en el servidor");
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        const container = document.getElementById('themes-container');
        if (container) container.innerHTML = "<p>Error al cargar skins.</p>";
    }
}

function renderInventory(skins) {
    const container = document.getElementById('themes-container');
    if (!container) return;

    // MAGIA: Ahora agrupamos por CAMPEÓN en la vista de Inventario
    const grouped = skins.reduce((acc, skin) => {
        const champ = skin.campeon || "Unknown";
        if (!acc[champ]) acc[champ] = [];
        acc[champ].push(skin);
        return acc;
    }, {});

    // Ordenamos los campeones alfabéticamente para que sea fácil buscarlos
    const sortedChamps = Object.keys(grouped).sort();

    container.innerHTML = sortedChamps.map(champ => `
        <div class="theme-group">
            <h3 class="theme-title" style="color: white; border-left: 4px solid var(--accent);">${champ}</h3>
            <div class="skins-row">
                ${grouped[champ].map(s => {
                    // FIX DEFINITIVO DE IMÁGENES
                    const champId = s.campeonId || "Unknown";
                    const skinIndex = parseInt(s.id) % 1000; 
                    const imgUrl = `https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${champId}_${skinIndex}.jpg`;

                    // Mostramos el nombre de la skin y abajo, en pequeño, a qué temática pertenece
                    return `
                        <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin('${s.id}', this)">
                            <div class="skin-img-wrapper">
                                <img src="${imgUrl}" onerror="this.src='https://via.placeholder.com/300x170/121214/FFFFFF?text=${encodeURIComponent(s.nombre)}'">
                            </div>
                            <div class="skin-name">
                                ${s.nombre}
                                <br>
                                <small style="color: var(--gold); font-size: 0.7rem; font-weight: 400;">${s.tema}</small>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `).join('');
}

async function toggleSkin(skinId, element) {
    element.style.pointerEvents = 'none'; 
    const isNowOwned = !element.classList.contains('owned');

    try {
        const res = await fetch(`${API_URL}/Rift/inventory/toggle`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: currentUser.userId, skinId: skinId, owned: isNowOwned })
        });
        
        if (res.ok) element.classList.toggle('owned');
        else alert("Error guardando. Revisa tu conexión.");
    } catch (error) {
        console.error("Error:", error);
    } finally {
        element.style.pointerEvents = 'auto';
    }
}

async function createNewLobby() {
    try {
        const response = await fetch(`${API_URL}/Lobby/create`, { method: 'POST' });
        const data = await response.json();
        await joinLobbyRequest(data.lobbyCode);
    } catch (e) { alert("Error al crear sala"); }
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
            
            if (!lobbyInterval) {
                lobbyInterval = setInterval(refreshTeamBuilder, 3000);
            }
        } else {
            alert("Sala llena o inexistente.");
        }
    } catch (e) { console.error("Error sala:", e); }
}

function copyInviteLink() {
    const url = `${window.location.origin}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(url);
    
    const btnElements = document.querySelectorAll('.btn-secondary');
    btnElements.forEach(btn => {
        if (btn.innerText.includes("Copiar")) {
            const originalText = btn.innerText;
            btn.innerText = "¡Enlace Copiado!";
            btn.style.borderColor = "var(--accent)";
            btn.style.color = "var(--accent)";
            
            setTimeout(() => {
                btn.innerText = originalText;
                btn.style.borderColor = "var(--border)";
                btn.style.color = "var(--text-main)";
            }, 2000);
        }
    });
}

async function refreshTeamBuilder() {
    if (!currentLobbyCode) return;
    try {
        const res = await fetch(`${API_URL}/Lobby/teambuilder/${currentLobbyCode}`);
        if (!res.ok) throw new Error("Error servidor");
        const data = await res.json();
        renderTeamBuilder(data);
    } catch (e) { console.error(e); }
}

function renderTeamBuilder(data) {
    const container = document.getElementById('team-results');
    if (!container) return;

    if (Object.keys(data).length === 0) {
        container.innerHTML = "<p style='text-align:center;'>Aún no hay coincidencias. Selecciona skins o invita amigos.</p>";
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

document.addEventListener('DOMContentLoaded', () => {
    if (!currentUser) {
        showView('view-auth');
    } else {
        const params = new URLSearchParams(window.location.search);
        const joinCode = params.get('join');
        
        if (joinCode) {
            joinLobbyRequest(joinCode);
        } else {
            showView('view-home');
        }
    }

    const handleEnterKey = (event) => {
        if (event.key === 'Enter') {
            handleAuth();
        }
    };
    
    const userPassInput = document.getElementById('auth-pass');
    const userNameInput = document.getElementById('auth-user');
    
    if (userPassInput) userPassInput.addEventListener('keypress', handleEnterKey);
    if (userNameInput) userNameInput.addEventListener('keypress', handleEnterKey);
});