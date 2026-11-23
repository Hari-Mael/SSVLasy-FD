using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sahavola
{
    public static class CodeEncoder
    {
        private static readonly Dictionary<char, string> Morse = new()
        {
            {'A',".-"},{'B',"-..."},{'C',"-.-."},{'D',"-.."},{'E',"."},
            {'F',"..-."},{'G',"--."},{'H',"...."},{'I',".."},{'J',".---"},
            {'K',"-.-"},{'L',".-.."},{'M',"--"},{'N',"-."},{'O',"---"},
            {'P',".--."},{'Q',"--.-"},{'R',".-."},{'S',"..."},{'T',"-"},
            {'U',"..-"},{'V',"...-"},{'W',".--"},{'X',"-..-"},{'Y',"-.--"},{'Z',"--.."}
        };

        private static readonly Dictionary<char, string> Otan = new()
        {
            {'A',"Alpha"},{'B',"Bravo"},{'C',"Charlie"},{'D',"Delta"},{'E',"Echo"},
            {'F',"Fox"},{'G',"Golf"},{'H',"Hotel"},{'I',"India"},{'J',"Juliette"},
            {'K',"Kilo"},{'L',"Lima"},{'M',"Mike"},{'N',"November"},{'O',"Oscar"},
            {'P',"Papa"},{'Q',"Québec"},{'R',"Roméo"},{'S',"Sierra"},{'T',"Tango"},
            {'U',"Uniforme"},{'V',"Victor"},{'W',"Whisky"},{'X',"X-Ray"},{'Y',"Yankee"},{'Z',"Zulu"}
        };

        private static readonly Dictionary<string, string> CodeQ = new()
        {
            {"QRA","Trano"},{"QRB","Elanelana"},{"QRD","Mandeha"},
            {"QRE","Ora nahatongavana"},{"QRG","Onjampeo"},{"QRI","Feo"},
            {"QRJ","Voaray tsara ve ?"},{"QRL","Sahirana"},{"QRM","Brouillage"},
            {"QRN","Orana"},{"QRO","Mafy tsara"},{"QRP","Kely"},
            {"QRQ","Miteny haingana"},{"QRR","Anaran'ny station"},{"QRRR","SOS vonjy"},
            {"QRS","Miteny miadana"},{"QRV","Vonona"},{"QRW","Afaka miantso"},
            {"QRX","Mijanona kely"},{"QRZ","Anarana"},{"QSA","Signal"},
            {"QSJ","Vola"},{"QSK","Afaka manohy ve"},{"QSL","Confirmation"},
            {"QSO","Resaka"},{"QSP","Hafatra"},{"QSY","Mifindra fantsona"},
            {"QTH","Toerana"},{"QTR","Ora"},{"QRT","Tapitra"},{"QRU","Tsy misy tenenina"}
        };

        private static readonly Dictionary<string, string> Chiffre = new()
        {
            {"99","QSY"},{"813","Zava-pisotro"},{"51","Miarahaba"},{"73","Am-pirahalahiana"},
            {"144","Matory"},{"1042","Loza"},{"318","WC"},{"1043","Embouteillage"},
            {"88","Oroka"},{"600","Telefaonina"},{"10","Soava-dia"}
        };

        private static readonly Dictionary<string, string> Lohateny = new()
        {
            {"AMI","A"},{"MAIZ","B"},{"IA","D"},{"MNF","E"},{"JRD","F"},{"AM","G"},
            {"M2A","H"},{"DL2H","I"},{"RTZ","J"},{"R2T","K"},{"ENV","L"},{"JODI","M"},
            {"MNM","N"},{"ORT","O"},{"AH","P"},{"ARV","R"},{"HSV","S"},{"NFRI","T"},
            {"MA2I","V"},{"2TI","Y"},{"DA","Z"}
        };

        public static readonly HashSet<string> CodesSupportes = new()
        {
            "Cesar", "Atbash", "Vigenere", "Polybe", "A1Z26", "Inverse",
            "Morse", "Morse lampe", "Morse sifflet", "OTAN", "Semaphore",
            "Montre Murale", "Pyramide", "Code Q", "Chiffre", "Lohateny", "Code-Code"
        };

        public static string Encode(string code, string message, string param)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            return code switch
            {
                "Cesar" => Cesar(message, ParseInt(param, 3)),
                "Atbash" => Atbash(message),
                "Vigenere" => Vigenere(message, param),
                "Polybe" => Polybe(message),
                "A1Z26" => A1Z26(message),
                "Inverse" => Reverse(message),
                "Morse" => MorseEncode(message),
                "Morse lampe" => "LAMPE: " + MorseEncode(message),
                "Morse sifflet" => "SIFFLET: " + MorseEncode(message),
                "OTAN" => OtanEncode(message),
                "Semaphore" => SemaphoreEncode(message),
                "Montre Murale" => MontreMurale(message),
                "Pyramide" => Pyramide(message),
                "Code Q" => MapperVersCode(message, CodeQ),
                "Chiffre" => MapperVersCode(message, Chiffre),
                "Lohateny" => MapperCaracteresVersCode(message, Lohateny),
                "Code-Code" => "DINGANA 1 OTAN / ÉTAPE 1 OTAN: " + OtanEncode(message)
                    + " → DINGANA 2 MORSE / ÉTAPE 2 MORSE: " + MorseEncode(message)
                    + " → DINGANA 3 VOANJO / ÉTAPE 3 VOANJO: jereo p.30 / voir p.30",
                _ => throw new NotSupportedException("Kaody tsy voaporofo / Code non pris en charge : " + code)
            };
        }

        private static int ParseInt(string s, int def) => int.TryParse(s, out int v) ? v : def;

        private static string Cesar(string t, int d)
        {
            var sb = new StringBuilder();
            foreach (char c in t)
            {
                if (char.IsLetter(c))
                {
                    int b = char.IsUpper(c) ? 65 : 97;
                    sb.Append((char)(b + ((c - b + d % 26 + 26) % 26)));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Atbash(string t)
        {
            var sb = new StringBuilder();
            foreach (char c in t)
            {
                if (char.IsLetter(c))
                {
                    int b = char.IsUpper(c) ? 65 : 97;
                    sb.Append((char)(b + 25 - (c - b)));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Vigenere(string t, string cle)
        {
            string k = new string((cle ?? "").ToUpperInvariant().Where(c => c >= 'A' && c <= 'Z').ToArray());
            if (k.Length == 0) k = "CLE";
            var sb = new StringBuilder();
            int j = 0;
            foreach (char c in t)
            {
                if (char.IsLetter(c))
                {
                    int s = k[j % k.Length] - 'A';
                    int b = char.IsUpper(c) ? 65 : 97;
                    sb.Append((char)(b + ((c - b + s) % 26)));
                    j++;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Polybe(string t)
        {
            const string ca = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
            var parts = new List<string>();
            foreach (char c in t.ToUpperInvariant())
            {
                char cc = c == 'J' ? 'I' : c;
                int p = ca.IndexOf(cc);
                if (p >= 0) parts.Add(((p / 5) + 1).ToString() + ((p % 5) + 1).ToString());
            }
            return string.Join(" ", parts);
        }

        private static string A1Z26(string t)
        {
            var parts = new List<string>();
            foreach (char c in t)
            {
                if (char.IsLetter(c)) parts.Add((char.ToUpperInvariant(c) - 64).ToString());
            }
            return string.Join(" ", parts);
        }

        private static string Reverse(string t)
        {
            char[] a = t.ToCharArray();
            Array.Reverse(a);
            return new string(a);
        }

        private static string MorseEncode(string t)
        {
            var parts = new List<string>();
            foreach (char c in t)
            {
                if (c == ' ') parts.Add("/");
                else if (Morse.TryGetValue(char.ToUpperInvariant(c), out string? v)) parts.Add(v);
            }
            return string.Join(" ", parts);
        }

        private static string OtanEncode(string t)
        {
            var parts = new List<string>();
            foreach (char c in t)
            {
                if (Otan.TryGetValue(char.ToUpperInvariant(c), out string? v)) parts.Add(v);
            }
            return string.Join(", ", parts);
        }

        private static string SemaphoreEncode(string t)
        {
            var parts = new List<string>();
            foreach (char c in t)
            {
                if (char.IsLetter(c))
                {
                    int index = char.ToUpperInvariant(c) - 'A' + 1;
                    parts.Add("SEM" + index.ToString("00"));
                }
            }
            return string.Join(" ", parts);
        }

        private static string MontreMurale(string t)
        {
            var map = new Dictionary<char, string> { {'A',"121"},{'M',"122"},{'Y',"123"},{'J',"91"},{'V',"92"},{'Q',"42"} };
            var parts = new List<string>();
            foreach (char c in t)
            {
                char u = char.ToUpperInvariant(c);
                parts.Add(map.TryGetValue(u, out string? v) ? v : "?");
            }
            return string.Join(" ", parts);
        }

        private static string Pyramide(string t)
        {
            string[] rows = { "A", "BC", "DEF", "GHIJ", "KLMNO", "PQRSTU", "VWXYZ12", "34567890" };
            var map = new Dictionary<char, string>();
            for (int ri = 0; ri < rows.Length; ri++)
            {
                for (int ci = 0; ci < rows[ri].Length; ci++)
                {
                    map[rows[ri][ci]] = (ri + 1).ToString() + (ci + 1).ToString();
                }
            }
            var parts = new List<string>();
            foreach (char c in t)
            {
                char u = char.ToUpperInvariant(c);
                if (u == ' ') parts.Add("/");
                else if (map.TryGetValue(u, out string? v)) parts.Add(v);
            }
            return string.Join("-", parts);
        }

        private static string MapperVersCode(string t, Dictionary<string, string> dictionnaire)
        {
            var inverse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entree in dictionnaire) inverse.TryAdd(entree.Value, entree.Key);

            string entier = t.Trim();
            if (inverse.TryGetValue(entier, out string? codeEntier)) return codeEntier;

            var parts = new List<string>();
            foreach (string mot in entier.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                parts.Add(inverse.TryGetValue(mot, out string? code) ? code : "[?]");
            }
            return string.Join(" | ", parts);
        }

        private static string MapperCaracteresVersCode(string t, Dictionary<string, string> dictionnaire)
        {
            var inverse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entree in dictionnaire) inverse.TryAdd(entree.Value, entree.Key);

            var parts = new List<string>();
            foreach (char c in t)
            {
                if (c == ' ') continue;
                parts.Add(inverse.TryGetValue(c.ToString(), out string? code) ? code : "[?]");
            }
            return string.Join(" ", parts);
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            string decompose = s.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in decompose)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
            }
            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static string Empreinte(string idActivite, string reponse, string cleHmac)
        {
            string normalisee = Normalize(reponse);
            if (normalisee.Length == 0) return "";
            byte[] cle = Convert.FromBase64String(cleHmac);
            byte[] message = Encoding.UTF8.GetBytes(idActivite + "|" + normalisee);
            return Convert.ToBase64String(HMACSHA256.HashData(cle, message));
        }

        public static bool VerifierReponse(Activite activite, string reponse, string cleHmac)
        {
            string candidate = Empreinte(activite.Id, reponse, cleHmac);
            if (candidate.Length == 0) return false;

            byte[] candidatOctets = Encoding.UTF8.GetBytes(candidate);
            bool trouvee = false;
            foreach (string attendue in activite.EmpreintesReponses)
            {
                if (CryptographicOperations.FixedTimeEquals(candidatOctets, Encoding.UTF8.GetBytes(attendue))) trouvee = true;
            }
            return trouvee;
        }
    }
}
