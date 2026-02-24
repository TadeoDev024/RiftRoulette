// Configuración de la URL de producción (Railway)
let isLoginMode = true;
const API_URL = "https://riftroulette-production.up.railway.app/api";

// Variables de estado global
let currentLobbyCode = "";
// Usuario de prueba si no hay uno logueado
let currentUser = JSON.parse(localStorage.getItem('user')) || { userId: 1, username: "admin" };
function showView(viewId) {
    // 1. Ocultamos todas las secciones que tengan la clase 'view'
    document.querySelectorAll('.view').forEach(v => {
        v.classList.remove('active');
        v.style.display = 'none';
    });

    // 2. Mostramos solo la sección que el usuario pidió
    const targetView = document.getElementById(viewId);
    if (targetView) {
        targetView.classList.add('active');
        targetView.style.display = 'block';
    }

    // 3. Lógica especial: Si va al inventario, cargamos los datos automáticamente
    if (viewId === 'view-inventory') {
        loadInventory();
    }
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
        if (container) container.innerHTML = "<p>Error al conectar con la API. Verifica que las tablas existan en la DB.</p>";
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
async function createNewLobby() {
    try {
        const response = await fetch(`${API_URL}/Lobby/create`, { 
            method: 'POST',
            headers: { 'Authorization': `Bearer ${currentUser.token}` } // Si usas JWT
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
    // CORRECCIÓN: window.location (en minúsculas) para evitar errores
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
function showView(viewId) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    document.getElementById(viewId).classList.add('active');
    
    // Solo mostrar Navbar si no estamos en login
    document.getElementById('main-nav').style.display = viewId === 'view-auth' ? 'none' : 'flex';

    if(viewId === 'view-inventory') loadInventory();
}

async function handleAuth() {
    const user = document.getElementById('auth-user').value;
    const pass = document.getElementById('auth-pass').value;

    // Conexión real con tu AuthController.cs
    try {
        const response = await fetch(`${API_URL}/auth/login`, {
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
            alert("Credenciales incorrectas");
        }
    } catch (e) {
        alert("Error de conexión con el servidor");
    }
}

function logout() {
    localStorage.clear();
    location.reload();
}

