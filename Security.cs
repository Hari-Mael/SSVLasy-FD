using System;
using System.Security.Cryptography;
using System.Text;

namespace Sahavola
{
    // Sécurité tenimiafina — Hash + Salt — tsy azo averina / irréversible
    // Sécurité mot de passe — Hash + Salt — irréversible
    public static class Security
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        // 1. Mamorona sira / Créer un sel aléatoire
        public static string NewSalt()
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        // 2. Hash : PBKDF2 SHA256 — 100 000 iterations
        public static string HashPassword(string password, string saltBase64)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Tenimiafina banga / Mot de passe vide");
            if (string.IsNullOrEmpty(saltBase64)) throw new ArgumentException("Sira banga / Sel vide");
            byte[] salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);
                return Convert.ToBase64String(hash);
            }
        }

        // 3. Fanamarinana / Vérification — tsy mampitaha tenimiafina mazava mihitsy
        //    Comparaison en temps constant — anti timing attack
        public static bool Verify(string passwordClair, string salt, string hashAttendu)
        {
            if (string.IsNullOrEmpty(passwordClair) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(hashAttendu))
                return false;
            try
            {
                string hashCalcule = HashPassword(passwordClair, salt);
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(hashCalcule),
                    Convert.FromBase64String(hashAttendu)
                );
            }
            catch
            {
                return false;
            }
        }

        // 4. Herin'ny tenimiafina / Force du mot de passe
        public static bool IsPasswordFort(string password, out string erreur)
        {
            erreur = "";
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                erreur = "Tenimiafina : 8 litera farafahakeliny / Mot de passe : 8 caractères minimum";
                return false;
            }
            return true;
        }

        // 5. Migration : raha misy Pass taloha (HTML) — avadika ho Hash avy hatrany
        //    Migration : si ancien Pass en clair — converti en Hash immédiatement
        public static (string hash, string salt) MigrateFromClair(string passClair)
        {
            string salt = NewSalt();
            string hash = HashPassword(passClair, salt);
            return (hash, salt);
        }
    }
}