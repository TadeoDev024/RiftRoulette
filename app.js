const API_URL = "https://riftroulette-production.up.railway.app/"; // URL generada en Railway

let currentLobbyCode = "";
let currentUser = JSON.parse(localStorage.getItem('user')) || null;

// Sincronizar skins (botón de pánico)
async function syncRiotData() {
    if (!confirm("¿Deseas sincronizar la base de datos con Riot Games?")) return;
    const response = await fetch(`${API_URL}/Rift/sync-data`);
    if (response.ok) alert("Skins actualizadas con éxito.");
}

// Gestión de Inventario
async function loadInventory() {
    if (!currentUser) return;
    const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
    const skins = await response.json();
    renderInventory(skins);
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
                    <div class="skin-card ${s.owned ? 'owned' : ''}" onclick="toggleSkin('${s.id}', this)">
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
    await fetch(`${API_URL}/Rift/inventory/toggle`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: currentUser.userId, skinId: skinId, owned: isOwned })
    });
}

// Lobby e Invitaciones
function copyInviteLink() {
    const url = `${window.location.origin}?join=${currentLobbyCode}`;
    navigator.clipboard.writeText(url);
    const note = document.getElementById('notification');
    note.classList.add('show');
    setTimeout(() => note.classList.remove('show'), 2500);
}