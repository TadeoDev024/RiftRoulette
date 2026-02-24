namespace RiftRoulette.Models {
    public class LoginRequest {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class SkinDTO {
        public int Id { get; set; }
        public int ChampionId { get; set; }
        public string Nombre { get; set; } = "";
        public string Url { get; set; } = "";
    }
}