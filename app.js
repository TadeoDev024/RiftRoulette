const API_URL = "https://riftroulette-production.up.railway.app/api";

let currentLobbyCode = "";
// Usuario de prueba si no hay uno logueado
let currentUser = JSON.parse(localStorage.getItem('user')) || { userId: 1, username: "admin" };

async function loadInventory() {
    try {
        const response = await fetch(`${API_URL}/Rift/skins/${currentUser.userId}`);
        if (!response.ok) throw new Error("Error en el servidor");
        const skins = await response.json();
        renderInventory(skins);
    } catch (error) {
        console.error("Error cargando inventario:", error);
        document.getElementById('themes-container').innerHTML = "<p>Error al conectar con la API.</p>";
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

    await fetch(`${API_URL}/Rift/inventory/toggle`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            userId: currentUser.userId, 
            skinId: skinId, 
            owned: isNowOwned 
        })
    });
}

// Cargar inventario al iniciar si estamos en la vista correcta
document.addEventListener('DOMContentLoaded', () => {
    loadInventory();
});