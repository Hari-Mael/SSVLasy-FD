using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sahavola
{
    public enum Role { Membre, Chef, Animateur, SuperAdmin }
    public enum StatutCompte { EnAttente, Valide, Suspendu }
    public enum StatutCampagne { Draft, Scheduled, Running, Paused, Finished }
    public enum Variante { Terrain, Virtuel, Hybride }
    public enum PorteeActivite { Tarika, Rehetra, CrossTarika }
    public enum TypeLasy { Tselatra, Lehibe, Feno }

    public sealed class Personnalisation
    {
        public string NomPersonnage { get; set; } = "";
        public string CouleurPeau { get; set; } = "#8D5524";
        public string CouleurCheveux { get; set; } = "#2C1A0E";
        public string StyleCheveux { get; set; } = "Court";
        public string CouleurYeux { get; set; } = "#4A2C0A";
        public float Taille { get; set; } = 1.75f;
        public string Genre { get; set; } = "Non précisé";
        public string Tenue { get; set; } = "Tenue Skoto Classique";
        public string CouleurTenue { get; set; } = "#2E7D32";
        public string Foulard { get; set; } = "Rouge";
        public string Chapeau { get; set; } = "Aucun";
        public string Arme { get; set; } = "Bâton";
        public string Sac { get; set; } = "Sac à dos";
        public string Accessoire { get; set; } = "Boussole";
        public string Voix { get; set; } = "Normale";
        public string EmotePreferee { get; set; } = "Salut";
        public string AvatarEmoji { get; set; } = "🧑";
        public string AvatarImagePath { get; set; } = "";
    }

    public sealed class Tarika
    {
        public string Id { get; set; } = "";
        public string Nom { get; set; } = "";
        public string ChefPseudo { get; set; } = "";
        public string Couleur { get; set; } = "#2E7D32";
        public string Embleme { get; set; } = "🦅";
        public List<string> Equipes { get; set; } = new();
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public string Devise { get; set; } = "";
    }

    public sealed class User
    {
        public string Pseudo { get; set; } = "";
        public string PassHash { get; set; } = "";
        public string Salt { get; set; } = "";
        public Role Role { get; set; } = Role.Membre;
        public string Equipe { get; set; } = "";
        public string RoleEquipe { get; set; } = "";
        public string Parrain { get; set; } = "";
        public StatutCompte Statut { get; set; } = StatutCompte.EnAttente;
        public string TarikaId { get; set; } = "";
        public Personnalisation Perso { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DernierLogin { get; set; }
        public int TentativesEchouees { get; set; } = 0;
        public int Energie { get; set; } = 100;
        public DateTime DerniereMaj { get; set; } = DateTime.UtcNow;
        public int Rations { get; set; }
        public DateTime? BloqueJusquA { get; set; }
        public bool ConsentementParental { get; set; }
        public string NomParent { get; set; } = "";
        public string ContactParent { get; set; } = "";
    }

    public sealed class Poi
    {
        public string Nom { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public bool Interdit { get; set; }
        public string Desc { get; set; } = "";
        public Vecteur3 Position3D { get; set; } = new Vecteur3(0f, 0f, 0f);
    }

    public sealed class Activite
    {
        public string Id { get; set; } = "";
        public string Titre { get; set; } = "";
        public string Modele { get; set; } = "";
        public string Code { get; set; } = "";
        public string Param { get; set; } = "3";
        public string Clair { get; set; } = "";
        public string TexteCode { get; set; } = "";
        [JsonIgnore]
        public List<string> Reponses { get; set; } = new();
        public List<string> EmpreintesReponses { get; set; } = new();
        public List<string> Indices { get; set; } = new();
        public int Points { get; set; }
        public int Jour { get; set; } = 1;
        public string HeureDebut { get; set; } = "09:00";
        public string HeureFin { get; set; } = "11:00";
        public string Lieu { get; set; } = "";
        public string Teknika { get; set; } = "";
        public int EssaisMax { get; set; } = 3;
        public Variante Variante { get; set; } = Variante.Hybride;
        public string CreePar { get; set; } = "";
        public string Statut { get; set; } = "brouillon";
        public Vecteur3 Position3D { get; set; } = new Vecteur3(0f, 0f, 0f);
        public string TarikaId { get; set; } = "";
        public string TarikaNom { get; set; } = "";
        public PorteeActivite Portee { get; set; } = PorteeActivite.Tarika;
        public bool EvenementCommun { get; set; }
        public bool DemandeCrossTarika { get; set; }
        public bool ActiviteNavale { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    }

    public enum Meteo { Chaleur, Pluvieux, Venteux, ChaudHumide, VenteuxHumide, ChaudSec }

    public sealed class Campagne
    {
        public int Jours { get; set; } = 3;
        public string Debut { get; set; } = "";
        public StatutCampagne Statut { get; set; } = StatutCampagne.Draft;
        public Meteo MeteoActuelle { get; set; } = Meteo.Chaleur;
        public int JourActuel { get; set; } = 1;
        public TypeLasy Type { get; set; } = TypeLasy.Tselatra;

        [JsonIgnore]
        public string NomType => Type switch
        {
            TypeLasy.Tselatra => "⚡ Lasy Tselatra",
            TypeLasy.Lehibe => "🏕️ Lasy Lehibe",
            TypeLasy.Feno => "🌲 Lasy Feno",
            _ => "⚡ Lasy Tselatra"
        };

        [JsonIgnore]
        public int JoursMax => Type switch
        {
            TypeLasy.Tselatra => 3,
            TypeLasy.Lehibe => 5,
            TypeLasy.Feno => 11,
            _ => 3
        };

        [JsonIgnore]
        public string DescriptionType => Type switch
        {
            TypeLasy.Tselatra => "1 → 3 andro — Haingana, tampoka / Rapide, éclair — Weekend, Fitsapana",
            TypeLasy.Lehibe => "1 → 5 andro — Lehibe, feno / Grand, complet — Vacances, Lasy lehibe",
            TypeLasy.Feno => "1 → 11 andro — Feno, tanteraka / Complet, total — Lasy feno, Campagne feno",
            _ => ""
        };

        public static TypeLasy TypeDepuisJours(int jours)
        {
            if (jours <= 3) return TypeLasy.Tselatra;
            if (jours <= 5) return TypeLasy.Lehibe;
            return TypeLasy.Feno;
        }

        public static bool JoursValidePourType(int jours, TypeLasy type)
        {
            if (jours < 1) return false;
            return type switch
            {
                TypeLasy.Tselatra => jours <= 3,
                TypeLasy.Lehibe => jours <= 5,
                TypeLasy.Feno => jours <= 11,
                _ => false
            };
        }
    }

    public sealed class AuditLog
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Pseudo { get; set; } = "";
        public Role Role { get; set; } = Role.Membre;
        public string Action { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    public sealed class TeamProgress
    {
        public string Cle { get; set; } = "";
        public int Score { get; set; }
        public List<string> Badges { get; set; } = new();
        public List<string> Terminees { get; set; } = new();
        public Dictionary<string, int> Essais { get; set; } = new();
        public Dictionary<string, DateTime> Verrous { get; set; } = new();
        public Dictionary<string, string> Choix { get; set; } = new();
        public int RapportJour { get; set; }
        public string RapportTexte { get; set; } = "";
        public int Kitay { get; set; }
        public int Rano { get; set; }
        public int Sakafo { get; set; }
        public List<string> ActionsDuJour { get; set; } = new();
        public int RepasManques { get; set; }
        public DateTime? BloqueDepuis { get; set; }
        public bool Malade { get; set; }
    }

    public sealed class SosEnregistre
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Equipe { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public sealed class SaveData
    {
        public string CleHmac { get; set; } = "";
        public List<User> Users { get; set; } = new();
        public List<Tarika> Tarikas { get; set; } = new();
        public List<Poi> Pois { get; set; } = new();
        public List<Activite> Activites { get; set; } = new();
        public List<TeamProgress> Progressions { get; set; } = new();
        public Campagne? Campagne { get; set; }
        public List<AuditLog> Audit { get; set; } = new();
        public SosEnregistre? DernierSos { get; set; }
    }

    public struct Vecteur3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vecteur3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public sealed class Teknika
    {
        public string Categorie { get; set; } = "";
        public string Titre { get; set; } = "";
        public string Page { get; set; } = "";
        public string DescMg { get; set; } = "";
        public string DescFr { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string UsageMg { get; set; } = "";
        public string UsageFr { get; set; } = "";
        public string UsageEn { get; set; } = "";
        public string Code { get; set; } = "";
    }
}
