using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sahavola
{
    public enum ResultatChargement { Absent, Ouvert, CleInvalideOuCorrompu }

    public static class SecureStorage
    {
        private static readonly byte[] Entete = Encoding.ASCII.GetBytes("SHV2");
        private const int TailleSel = 16;
        private const int TailleNonce = 12;
        private const int TailleTag = 16;
        private const int TailleCle = 32;
        private const int Iterations = 600_000;
        private const int DebutCorps = 4 + TailleSel + TailleNonce + TailleTag;

        private static byte[]? _sel;
        private static byte[]? _cle;

        public static void Initialiser(string secret)
        {
            Fermer();
            _sel = RandomNumberGenerator.GetBytes(TailleSel);
            _cle = Deriver(secret, _sel);
        }

        public static ResultatChargement TryLoad(string chemin, string secret, out string json)
        {
            json = "";
            if (!File.Exists(chemin)) return ResultatChargement.Absent;

            byte[] fichier = File.ReadAllBytes(chemin);
            if (fichier.Length < DebutCorps || !fichier.AsSpan(0, Entete.Length).SequenceEqual(Entete.AsSpan()))
                return ResultatChargement.CleInvalideOuCorrompu;

            byte[] sel = fichier.AsSpan(Entete.Length, TailleSel).ToArray();
            byte[] nonce = fichier.AsSpan(Entete.Length + TailleSel, TailleNonce).ToArray();
            byte[] tag = fichier.AsSpan(Entete.Length + TailleSel + TailleNonce, TailleTag).ToArray();
            byte[] chiffre = fichier.AsSpan(DebutCorps).ToArray();

            byte[] cle = Deriver(secret, sel);
            byte[] clair = new byte[chiffre.Length];
            try
            {
                using var gcm = new AesGcm(cle);
                gcm.Decrypt(nonce, chiffre, tag, clair, Entete);
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(cle);
                return ResultatChargement.CleInvalideOuCorrompu;
            }

            Fermer();
            _sel = sel;
            _cle = cle;
            json = Encoding.UTF8.GetString(clair);
            CryptographicOperations.ZeroMemory(clair);
            return ResultatChargement.Ouvert;
        }

        public static void Enregistrer(string chemin, string json)
        {
            if (_cle == null || _sel == null) throw new InvalidOperationException("Stockage non ouvert / Stockage non ouvert");

            byte[] nonce = RandomNumberGenerator.GetBytes(TailleNonce);
            byte[] clair = Encoding.UTF8.GetBytes(json);
            byte[] chiffre = new byte[clair.Length];
            byte[] tag = new byte[TailleTag];
            using (var gcm = new AesGcm(_cle))
            {
                gcm.Encrypt(nonce, clair, chiffre, tag, Entete);
            }

            byte[] fichier = new byte[DebutCorps + chiffre.Length];
            int position = 0;
            Entete.CopyTo(fichier, position); position += Entete.Length;
            _sel.CopyTo(fichier, position); position += TailleSel;
            nonce.CopyTo(fichier, position); position += TailleNonce;
            tag.CopyTo(fichier, position); position += TailleTag;
            chiffre.CopyTo(fichier, position);

            string temporaire = chemin + ".tmp";
            File.WriteAllBytes(temporaire, fichier);
            File.Move(temporaire, chemin, true);
            CryptographicOperations.ZeroMemory(clair);
        }

        public static void Fermer()
        {
            if (_cle != null) CryptographicOperations.ZeroMemory(_cle);
            _cle = null;
            _sel = null;
        }

        private static byte[] Deriver(string secret, byte[] sel) =>
            Rfc2898DeriveBytes.Pbkdf2(secret, sel, Iterations, HashAlgorithmName.SHA256, TailleCle);
    }
}
