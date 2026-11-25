using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Sahavola
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; } = null!;
        public SaveData Data { get; private set; } = new SaveData();
        public User? CurrentUser { get; private set; }
        public bool LectureSeule { get; private set; }
        public string? ErreurStockage { get; private set; }

        public const int QUOTA_ASA_REHETRA_PAR_SEMAINE = 2;
        private const string CheminDonnees = "user://sahavola.dat";
        private const int TentativesAvantVerrou = 5;
        private const int MinutesVerrouCompte = 15;
        private const int MaxChefs = 6;
        private const int EssaisMaxAutorises = 3;
        private const int RationsParJour = 2;
        private const int EnergieMax = 100;
        private const int EnergieParRepas = 40;
        private static readonly int[] DureesVerrouEssaiMinutes = { 15, 10, 5 };
        private readonly Random _aleatoire = new();
        private const int MaxTarikas = 6;
        private const int MaxEquipesParChef = 3;
        private const int MembresParEquipe = 5;
        private const int MaxJoursCampagne = 11;
        private static readonly JsonSerializerOptions OptionsJson = new();
        private static readonly Dictionary<StatutCampagne, StatutCampagne[]> TransitionsCampagne = new()
        {
            [StatutCampagne.Draft] = new[] { StatutCampagne.Scheduled },
            [StatutCampagne.Scheduled] = new[] { StatutCampagne.Running, StatutCampagne.Draft },
            [StatutCampagne.Running] = new[] { StatutCampagne.Paused, StatutCampagne.Finished },
            [StatutCampagne.Paused] = new[] { StatutCampagne.Running, StatutCampagne.Finished },
            [StatutCampagne.Finished] = Array.Empty<StatutCampagne>()
        };
        private string _secret = "";
        private string _chemin = "";

        public override void _Ready() => Instance = this;

        public void SetSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                ErreurStockage = "Lakile banga ‖ Clé vide ‖ Key empty";
                return;
            }
            _secret = secret;
            _chemin = ProjectSettings.GlobalizePath(CheminDonnees);
            Load();
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(_secret)) return;

            ResultatChargement resultat = SecureStorage.TryLoad(_chemin, _secret, out string json);
            switch (resultat)
            {
                case ResultatChargement.Absent:
                    SecureStorage.Initialiser(_secret);
                    Data = new SaveData();
                    NormaliserDonnees();
                    LectureSeule = false;
                    ErreurStockage = null;
                    Save();
                    break;
                case ResultatChargement.Ouvert:
                    Data = JsonSerializer.Deserialize<SaveData>(json, OptionsJson) ?? new SaveData();
                    LectureSeule = false;
                    ErreurStockage = null;
                    if (NormaliserDonnees()) Save();
                    break;
                default:
                    Data = new SaveData();
                    LectureSeule = true;
                    ErreurStockage = "Lakile diso — tsy voatahiry ‖ Clé incorrecte — sauvegarde désactivée ‖ Wrong key — saving disabled";
                    break;
            }
        }

        public void Save()
        {
            if (LectureSeule || string.IsNullOrEmpty(_chemin)) return;
            SecureStorage.Enregistrer(_chemin, JsonSerializer.Serialize(Data, OptionsJson));
        }

        private bool NormaliserDonnees()
        {
            Data.Users ??= new List<User>();
            Data.Tarikas ??= new List<Tarika>();
            Data.Pois ??= new List<Poi>();
            Data.Activites ??= new List<Activite>();
            Data.Progressions ??= new List<TeamProgress>();
            Data.Audit ??= new List<AuditLog>();
            if (!string.IsNullOrEmpty(Data.CleHmac)) return false;
            Data.CleHmac = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            return true;
        }

        private void AddAudit(string action, string detail)
        {
            Data.Audit.Add(new AuditLog
            {
                Date = DateTime.UtcNow,
                Pseudo = CurrentUser?.Pseudo ?? "Système",
                Role = CurrentUser?.Role ?? Role.Membre,
                Action = action,
                Detail = detail
            });
            Save();
        }

        public List<AuditLog> GetAudit()
        {
            if (CurrentUser == null) return new List<AuditLog>();
            if (CurrentUser.Role == Role.SuperAdmin) return new List<AuditLog>(Data.Audit);

            var res = new List<AuditLog>();
            foreach (AuditLog l in Data.Audit)
            {
                bool visible = l.Pseudo == CurrentUser.Pseudo
                    || CurrentUser.Role == Role.Animateur
                    || (CurrentUser.Role == Role.Chef
                        && Data.Users.Exists(u => u.Pseudo == l.Pseudo && u.Parrain == CurrentUser.Pseudo));
                if (visible) res.Add(l);
            }
            return res;
        }

        public string CreateFirstSuperAdmin(string pseudo, string pass)
        {
            if (Data.Users.Count > 0) return "❌ Efa misy kaonty ‖ ❌ Un compte existe déjà ‖ ❌ An account already exists";
            string p = (pseudo ?? "").Trim();
            if (p.Length == 0) return "❌ Anarana banga ‖ ❌ Pseudo vide ‖ ❌ Username empty";
            if (!Security.IsPasswordFort(pass, out string err)) return "❌ " + err;

            string salt = Security.NewSalt();
            Data.Users.Add(new User
            {
                Pseudo = p,
                PassHash = Security.HashPassword(pass, salt),
                Salt = salt,
                Role = Role.SuperAdmin,
                Statut = StatutCompte.Valide,
                DateCreation = DateTime.UtcNow,
                Perso = new Personnalisation { NomPersonnage = p, AvatarEmoji = "👑" }
            });
            AddAudit("CREATE_FIRST_SUPERADMIN", p);
            return "✅ Mpitantana Ambony voalohany noforonina / Premier Super Admin créé : " + p;
        }

        public string Login(string pseudo, string pass)
        {
            string identifiant = (pseudo ?? "").Trim();
            User? u = Data.Users.Find(x => x.Pseudo.Equals(identifiant, StringComparison.OrdinalIgnoreCase));
            if (u == null) return "❌ Tenimiafina diso ‖ ❌ Identifiants incorrects ‖ ❌ Incorrect credentials";

            if (u.BloqueJusquA is DateTime fin && fin > DateTime.UtcNow)
            {
                int minutes = (int)Math.Ceiling((fin - DateTime.UtcNow).TotalMinutes);
                return $"❌ Kaonty voahidy {minutes} min ‖ ❌ Compte verrouillé {minutes} min ‖ ❌ Account locked for {minutes} min";
            }

            if (!Security.Verify(pass, u.Salt, u.PassHash))
            {
                u.TentativesEchouees++;
                if (u.TentativesEchouees >= TentativesAvantVerrou)
                {
                    u.BloqueJusquA = DateTime.UtcNow.AddMinutes(MinutesVerrouCompte);
                    u.TentativesEchouees = 0;
                    AddAudit("LOGIN_ECHEC", u.Pseudo + " verrouillé");
                    return $"❌ Kaonty voahidy {MinutesVerrouCompte} min ‖ ❌ Compte verrouillé {MinutesVerrouCompte} min ‖ ❌ Account locked for {MinutesVerrouCompte} min";
                }
                AddAudit("LOGIN_ECHEC", u.Pseudo);
                return "❌ Tenimiafina diso ‖ ❌ Identifiants incorrects ‖ ❌ Incorrect credentials";
            }

            if (u.Statut == StatutCompte.EnAttente) return "⏳ Miandry fanamarinana ‖ ⏳ En attente de validation ‖ ⏳ Waiting for approval";
            if (u.Statut == StatutCompte.Suspendu) return "❌ Kaonty mihantona ‖ ❌ Compte suspendu ‖ ❌ Account suspended";

            u.TentativesEchouees = 0;
            u.BloqueJusquA = null;
            u.DernierLogin = DateTime.UtcNow;
            CurrentUser = u;
            AddAudit("LOGIN", u.Pseudo);
            return "✅ Tongasoa / Bienvenue " + u.Pseudo + " — " + u.Role;
        }

        public void Logout()
        {
            if (CurrentUser != null) AddAudit("LOGOUT", CurrentUser.Pseudo);
            CurrentUser = null;
        }

        public bool CheckQuota(string equipe, string parrain, out string err)
        {
            err = "";
            string nomEquipe = (equipe ?? "").Trim();
            string chef = parrain ?? "";
            if (nomEquipe.Length == 0) { err = "Ekipa banga ‖ Équipe vide ‖ Team empty"; return false; }

            var equipesDuChef = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int membresDansEquipe = 0;
            foreach (User u in Data.Users)
            {
                if (u.Role != Role.Membre || u.Parrain != chef) continue;
                equipesDuChef.Add(u.Equipe);
                if (string.Equals(u.Equipe, nomEquipe, StringComparison.OrdinalIgnoreCase)) membresDansEquipe++;
            }
            if (!equipesDuChef.Contains(nomEquipe) && equipesDuChef.Count >= MaxEquipesParChef)
            {
                err = "Ekipa 3 farafahabetsany isaky ny mpitarika ‖ 3 équipes max par chef ‖ Max 3 teams per chief";
                return false;
            }
            if (membresDansEquipe >= MembresParEquipe)
            {
                err = "Ekipa feno (5/5) ‖ Équipe pleine (5/5) ‖ Team full (5/5)";
                return false;
            }
            return true;
        }

        public string CreateUser(string pseudo, string pass, Role role, string equipe, string roleEquipe, string parrain, bool validerDirect, bool consentement, string nomParent, string contactParent)
        {
            string p = (pseudo ?? "").Trim();
            if (p.Length == 0) return "❌ Anarana banga ‖ ❌ Pseudo vide ‖ ❌ Username empty";
            if (p.Length > 32) return "❌ Anarana lava loatra ‖ ❌ Pseudo trop long (32 max) ‖ ❌ Username too long (32 max)";
            if (Data.Users.Exists(u => u.Pseudo.Equals(p, StringComparison.OrdinalIgnoreCase))) return "❌ Anarana efa misy ‖ ❌ Pseudo déjà pris ‖ ❌ Username already taken";
            if (!Security.IsPasswordFort(pass, out string errPass)) return "❌ " + errPass;

            bool superAdmin = CurrentUser?.Role == Role.SuperAdmin;
            if (role == Role.SuperAdmin && !superAdmin) return "❌ Mpitantana Ambony ihany no afaka mamorona Mpitantana Ambony ‖ ❌ Seul un Super Admin peut créer un Super Admin ‖ ❌ Only a Super Admin can create a Super Admin";
            if (role == Role.SuperAdmin && Data.Users.Exists(u => u.Role == Role.SuperAdmin)) return "❌ Mpitantana Ambony iray ihany ‖ ❌ Un seul Super Admin ‖ ❌ Only one Super Admin";
            if (role == Role.Chef && Data.Users.Count(u => u.Role == Role.Chef) >= MaxChefs) return "❌ Mpitarika 6 farafahakeliny ‖ ❌ 6 chefs maximum ‖ ❌ 6 chiefs maximum";
            bool valide = validerDirect && superAdmin;

            if (role == Role.Membre)
            {
                if (!consentement) return "❌ Tsy maintsy misy fanomezan-dalan'ny ray aman-dreny ‖ ❌ Consentement parental obligatoire ‖ ❌ Parental consent required";
                if (string.IsNullOrWhiteSpace(nomParent) || string.IsNullOrWhiteSpace(contactParent))
                    return "❌ Anaran'ny ray aman-dreny sy contact angatahina ‖ ❌ Nom et contact du parent obligatoires ‖ ❌ Parent's name and contact required";
                string chef = (parrain ?? "").Trim();
                if (!Data.Users.Exists(u => u.Role == Role.Chef && u.Pseudo == chef)) return "❌ Mpitarika tsy hita ‖ ❌ Chef introuvable ‖ ❌ Chief not found";
                if (!CheckQuota(equipe, chef, out string errQuota)) return "❌ " + errQuota;
            }

            string salt = Security.NewSalt();
            bool estMembre = role == Role.Membre;
            var nouveau = new User
            {
                Pseudo = p,
                PassHash = Security.HashPassword(pass, salt),
                Salt = salt,
                Role = role,
                Equipe = estMembre ? (equipe ?? "").Trim() : "",
                RoleEquipe = estMembre ? (roleEquipe ?? "").Trim() : role.ToString(),
                Parrain = estMembre ? (parrain ?? "").Trim() : "",
                Statut = valide ? StatutCompte.Valide : StatutCompte.EnAttente,
                ConsentementParental = estMembre && consentement,
                NomParent = estMembre ? (nomParent ?? "").Trim() : "",
                ContactParent = estMembre ? (contactParent ?? "").Trim() : "",
                DateCreation = DateTime.UtcNow,
                Perso = new Personnalisation
                {
                    NomPersonnage = p,
                    AvatarEmoji = role == Role.Chef ? "🧭" : role == Role.Animateur ? "🎭" : "🧑"
                }
            };

            if (estMembre)
            {
                Tarika? tarikaDuChef = Data.Tarikas.Find(t => t.ChefPseudo == nouveau.Parrain);
                if (tarikaDuChef != null) nouveau.TarikaId = tarikaDuChef.Id;
            }
            else if (role == Role.Chef)
            {
                Tarika? tarikaPropre = Data.Tarikas.Find(t => t.ChefPseudo == p);
                if (tarikaPropre != null) nouveau.TarikaId = tarikaPropre.Id;
            }

            Data.Users.Add(nouveau);
            AddAudit("CREATE_USER", $"{p} {role} parrain={nouveau.Parrain} statut={nouveau.Statut}");
            return valide
                ? "✅ " + p + " noforonina sy voamarina / créé et validé"
                : "⏳ " + p + " — Fangatahana nalefa, miandry fanamarinana / Demande envoyée, en attente";
        }

        public bool PeutVoirUser(User viewer, User cible)
        {
            if (viewer.Role == Role.SuperAdmin || viewer.Role == Role.Animateur) return true;
            if (viewer.Role == Role.Chef) return cible.Parrain == viewer.Pseudo || cible.Pseudo == viewer.Pseudo;
            return cible.Pseudo == viewer.Pseudo;
        }

        public string ValiderUser(string pseudo, bool valider)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany ‖ ❌ Seul Super Admin ‖ ❌ Super Admin only";
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            if (u == null) return "❌ Kaonty tsy hita ‖ ❌ Compte introuvable ‖ ❌ Account not found";
            u.Statut = valider ? StatutCompte.Valide : StatutCompte.Suspendu;
            AddAudit(valider ? "VALIDER_USER" : "REFUSER_USER", pseudo);
            return valider ? "✅ " + pseudo + " voamarina / validé" : "❌ " + pseudo + " nolavina / refusé";
        }

        public string SuspendreUser(string pseudo)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany";
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            if (u == null) return "❌ Kaonty tsy hita";
            if (u.Pseudo == CurrentUser.Pseudo) return "❌ Tsy azo atao ny manala ny tenanao ‖ ❌ Impossible de te suspendre toi-même ‖ ❌ You cannot suspend yourself";
            u.Statut = StatutCompte.Suspendu;
            AddAudit("SUSPENDRE_USER", pseudo);
            return "✅ " + pseudo + " mihantona / suspendu";
        }

        public string SupprimerUser(string pseudo)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany";
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            if (u == null) return "❌ Kaonty tsy hita";
            if (u.Pseudo == CurrentUser.Pseudo) return "❌ Tsy azo atao ny mamafa ny tenanao ‖ ❌ Tu ne peux pas supprimer ton propre compte ‖ ❌ You cannot delete your own account";
            if (u.Role == Role.SuperAdmin && Data.Users.Count(x => x.Role == Role.SuperAdmin) <= 1)
                return "❌ Tsy azo fafana ny Mpitantana Ambony farany ‖ ❌ Impossible de supprimer le dernier Super Admin ‖ ❌ Cannot delete the last Super Admin";

            if (u.Role == Role.Chef)
            {
                List<string> idsTarikas = Data.Tarikas.Where(x => x.ChefPseudo == u.Pseudo).Select(x => x.Id).ToList();
                foreach (string id in idsTarikas) DetacherTarika(id);
            }
            Data.Users.Remove(u);
            AddAudit("SUPPRIMER_USER", pseudo);
            return "✅ " + pseudo + " voafafa / supprimé";
        }

        public string CreerTarika(string nom, string chefPseudo, string couleur, string embleme, string devise)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany ‖ ❌ Seul le Super Admin ‖ ❌ Super Admin only";
            string nomNettoye = (nom ?? "").Trim();
            if (nomNettoye.Length == 0) return "❌ Anaran'ny tarika banga ‖ ❌ Nom du groupe vide ‖ ❌ Group name empty";
            if (Data.Tarikas.Count >= MaxTarikas) return "❌ Tarika 6 farafahabetsany ‖ ❌ 6 groupes maximum ‖ ❌ 6 groups maximum";
            if (Data.Tarikas.Exists(t => t.ChefPseudo == chefPseudo)) return "❌ Efa manana tarika io mpitarika io ‖ ❌ Ce chef a déjà un groupe ‖ ❌ This leader already has a group";
            User? chef = Data.Users.Find(u => u.Pseudo == chefPseudo);
            if (chef == null || chef.Role != Role.Chef) return "❌ Mpitarika tsy hita ‖ ❌ Chef introuvable ‖ ❌ Chief not found";

            var nouvelleTarika = new Tarika
            {
                Id = Guid.NewGuid().ToString("N"),
                Nom = nomNettoye,
                ChefPseudo = chefPseudo,
                Couleur = couleur,
                Embleme = embleme,
                Devise = (devise ?? "").Trim(),
                DateCreation = DateTime.UtcNow
            };
            Data.Tarikas.Add(nouvelleTarika);
            chef.TarikaId = nouvelleTarika.Id;
            foreach (User membre in Data.Users)
            {
                if (membre.Parrain == chefPseudo && string.IsNullOrEmpty(membre.TarikaId)) membre.TarikaId = nouvelleTarika.Id;
            }
            AddAudit("CREATE_TARIKA", $"{nomNettoye} chef={chefPseudo}");
            return "✅ Tarika noforonina / Groupe créé : " + nomNettoye + " " + embleme;
        }

        public Tarika? GetTarikaDe(string pseudo)
        {
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            if (u == null || string.IsNullOrEmpty(u.TarikaId)) return null;
            return Data.Tarikas.Find(t => t.Id == u.TarikaId);
        }

        public List<User> GetMembresTarika(string tarikaId) => Data.Users.Where(u => u.TarikaId == tarikaId).ToList();

        public int CompterTarika(string tarikaId) => GetMembresTarika(tarikaId).Count;

        public string SupprimerTarika(string tarikaId)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany";
            Tarika? t = Data.Tarikas.Find(x => x.Id == tarikaId);
            if (t == null) return "❌ Tarika tsy hita ‖ ❌ Groupe introuvable ‖ ❌ Group not found";
            string nom = t.Nom;
            DetacherTarika(tarikaId);
            AddAudit("DELETE_TARIKA", nom);
            return "✅ Tarika voafafa / Groupe supprimé : " + nom;
        }

        private void DetacherTarika(string tarikaId)
        {
            Data.Tarikas.RemoveAll(t => t.Id == tarikaId);
            foreach (User u in Data.Users)
            {
                if (u.TarikaId == tarikaId) u.TarikaId = "";
            }
        }

        public string SauverPersonnalisation(string pseudo, Personnalisation perso)
        {
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            if (u == null) return "❌ Kaonty tsy hita ‖ ❌ Compte introuvable ‖ ❌ Account not found";
            if (CurrentUser == null || (CurrentUser.Pseudo != pseudo && CurrentUser.Role != Role.SuperAdmin))
                return "❌ Ny anao ihany no afaka ovaina ‖ ❌ Tu ne peux modifier que le tien ‖ ❌ You can only edit your own";

            if (string.IsNullOrWhiteSpace(perso.NomPersonnage)) perso.NomPersonnage = pseudo;
            perso.Taille = float.IsNaN(perso.Taille) ? 1.75f : Math.Clamp(perso.Taille, 1.5f, 2.0f);
            perso.AvatarImagePath = "";
            u.Perso = perso;
            AddAudit("SAVE_PERSO", pseudo);
            return "✅ Mpilalao voatahiry / Joueur sauvegardé : " + perso.NomPersonnage + " " + perso.AvatarEmoji;
        }

        public Personnalisation GetPerso(string pseudo)
        {
            User? u = Data.Users.Find(x => x.Pseudo == pseudo);
            return u?.Perso ?? new Personnalisation { NomPersonnage = pseudo, AvatarEmoji = "🧑" };
        }

        public string CreerCampagne(int jours, string debut, TypeLasy? typeChoisi = null)
        {
            if (CurrentUser == null || (CurrentUser.Role != Role.SuperAdmin && CurrentUser.Role != Role.Chef))
                return "❌ Mpitantana Ambony na Mpitarika ihany ‖ ❌ Seul Super Admin ou Chef ‖ ❌ Super Admin or Chief only";
            if (Data.Campagne != null && Data.Campagne.Statut != StatutCampagne.Finished)
                return "❌ Efa misy lasy mandeha ‖ ❌ Une campagne est déjà en cours ‖ ❌ A camp is already running";
            if (jours < 1 || jours > MaxJoursCampagne) return "❌ 1 ka hatramin'ny 11 andro ‖ ❌ 1 à 11 jours ‖ ❌ 1 to 11 days";
            if (!DateOnly.TryParseExact(debut, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return "❌ Daty diso (AAAA-MM-JJ) ‖ ❌ Date invalide (AAAA-MM-JJ) ‖ ❌ Invalid date (YYYY-MM-DD)";

            TypeLasy type = typeChoisi ?? Campagne.TypeDepuisJours(jours);
            if (!Campagne.JoursValidePourType(jours, type))
            {
                int max = new Campagne { Type = type }.JoursMax;
                return $"❌ {jours} andro dia mihoatra ny {max} andro max — fidio sokajy hafa na ahenao ny andro ‖ ❌ {jours} jours dépassent {max} jours max — choisis un autre type ou réduis les jours ‖ ❌ {jours} days exceed the {max}-day max — choose another type or reduce the days";
            }

            Data.Campagne = new Campagne { Jours = jours, Debut = debut, Statut = StatutCampagne.Scheduled, JourActuel = 1, Type = type };
            AddAudit("CREATE_CAMPAGNE", $"{type} {jours} jours debut={debut}");
            return $"✅ {Data.Campagne.NomType} noforonina : {jours} andro — {Data.Campagne.DescriptionType} ‖ ✅ {Data.Campagne.NomType} créée : {jours} jours — {Data.Campagne.DescriptionType} ‖ ✅ {Data.Campagne.NomType} created: {jours} days — {Data.Campagne.DescriptionType}";
        }

        public string ChangerTypeLasy(TypeLasy nouveauType)
        {
            if (CurrentUser == null || (CurrentUser.Role != Role.SuperAdmin && CurrentUser.Role != Role.Chef)) return "❌ Mpitantana Ambony na Mpitarika ihany ‖ ❌ Seul Super Admin ou Chef ‖ ❌ Super Admin or Chief only";
            if (Data.Campagne == null) return "❌ Tsy misy lasy ‖ ❌ Aucune campagne ‖ ❌ No camp";
            if (Data.Campagne.Statut != StatutCampagne.Draft && Data.Campagne.Statut != StatutCampagne.Scheduled)
                return "❌ Tsy azo ovaina rehefa efa nanomboka ‖ ❌ Impossible à changer une fois la campagne lancée ‖ ❌ Cannot be changed once the camp has started";
            if (!Campagne.JoursValidePourType(Data.Campagne.Jours, nouveauType))
                return $"❌ Tsy mifanaraka — {Data.Campagne.Jours} andro dia mihoatra ny max ho an'ity sokajy ity ‖ ❌ Incompatible — {Data.Campagne.Jours} jours dépassent le max pour ce type ‖ ❌ Incompatible — {Data.Campagne.Jours} days exceed the max for this type";
            Data.Campagne.Type = nouveauType;
            AddAudit("CHANGER_TYPE_LASY", nouveauType.ToString());
            return "✅ Sokajin'ny lasy novaina / Type de camp changé : " + Data.Campagne.NomType;
        }

        public string ChangerStatutCampagne(StatutCampagne cible)
        {
            if (CurrentUser == null || (CurrentUser.Role != Role.SuperAdmin && CurrentUser.Role != Role.Chef))
                return "❌ Mpitantana Ambony na Mpitarika ihany ‖ ❌ Seul Super Admin ou Chef ‖ ❌ Super Admin or Chief only";
            if (Data.Campagne == null) return "❌ Tsy misy lasy ‖ ❌ Aucune campagne ‖ ❌ No camp";
            if (!TransitionsCampagne[Data.Campagne.Statut].Contains(cible))
                return $"❌ Tsy azo atao ny fiovana {Data.Campagne.Statut} → {cible} ‖ ❌ Transition interdite : {Data.Campagne.Statut} → {cible} ‖ ❌ Transition not allowed: {Data.Campagne.Statut} → {cible}";
            if (cible == StatutCampagne.Running && Data.Campagne.Statut == StatutCampagne.Scheduled) DistribuerRations();
            Data.Campagne.Statut = cible;
            AddAudit("CHANGER_CAMPAGNE", cible.ToString());
            return "✅ Lasy : " + cible;
        }

        public string ChangerMeteo(Meteo nouvelle)
        {
            if (CurrentUser == null || (CurrentUser.Role != Role.SuperAdmin && CurrentUser.Role != Role.Chef))
                return "❌ Mpitantana Ambony na Mpitarika ihany ‖ ❌ Seul Super Admin ou Chef ‖ ❌ Super Admin or Chief only";
            if (Data.Campagne == null) return "❌ Tsy misy lasy ‖ ❌ Aucune campagne ‖ ❌ No camp";
            Data.Campagne.MeteoActuelle = nouvelle;
            AddAudit("CHANGER_METEO", nouvelle.ToString());
            return "✅ Toetr'andro / Météo : " + nouvelle;
        }

        public int EnergieDe(User utilisateur)
        {
            ActualiserEnergie(utilisateur);
            return utilisateur.Energie;
        }

        public string Manger()
        {
            if (CurrentUser == null) return "❌ Midira aloha ‖ ❌ Connecte-toi d'abord ‖ ❌ Log in first";
            ActualiserEnergie(CurrentUser);
            if (CurrentUser.Rations <= 0) return "❌ Tsy misy sakafo ‖ ❌ Plus de rations ‖ ❌ No rations left";
            if (CurrentUser.Energie >= EnergieMax) return "✅ Feno ny hery ‖ ✅ Énergie déjà au maximum ‖ ✅ Energy already at maximum";
            CurrentUser.Rations--;
            CurrentUser.Energie = Math.Min(EnergieMax, CurrentUser.Energie + EnergieParRepas);
            CurrentUser.DerniereMaj = DateTime.UtcNow;
            AddAudit("MANGER", CurrentUser.Pseudo);
            return "✅ Nihinana / Repas pris : énergie " + CurrentUser.Energie + "/" + EnergieMax;
        }

        private void ActualiserEnergie(User utilisateur)
        {
            DateTime maintenant = DateTime.UtcNow;
            double minutes = (maintenant - utilisateur.DerniereMaj).TotalMinutes;
            double tauxParMinute = TauxEnergie(Data.Campagne?.MeteoActuelle ?? Meteo.Chaleur);
            int perdu = (int)Math.Floor(minutes * tauxParMinute);
            if (perdu <= 0) return;
            utilisateur.Energie = Math.Max(0, utilisateur.Energie - perdu);
            utilisateur.DerniereMaj = maintenant;
        }

        private static double TauxEnergie(Meteo meteo) => meteo switch
        {
            Meteo.Chaleur or Meteo.ChaudHumide or Meteo.ChaudSec => 0.15,
            _ => 0.1
        };

        private Meteo TirerMeteo()
        {
            Meteo[] valeurs = Enum.GetValues<Meteo>();
            return valeurs[_aleatoire.Next(valeurs.Length)];
        }

        private void DistribuerRations()
        {
            foreach (User utilisateur in Data.Users.Where(u => u.Statut == StatutCompte.Valide))
            {
                utilisateur.Rations += RationsParJour;
            }
        }

        public string AvancerJour()
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany ‖ ❌ Seul Super Admin ‖ ❌ Super Admin only";
            if (Data.Campagne == null || Data.Campagne.Statut != StatutCampagne.Running) return "❌ Tsy mandeha ny lasy ‖ ❌ Campagne non démarrée ‖ ❌ Camp not started";
            if (Data.Campagne.JourActuel >= Data.Campagne.Jours) return "❌ Farany ny andro ‖ ❌ Dernier jour atteint ‖ ❌ Last day reached";
            Data.Campagne.JourActuel++;
            Data.Campagne.MeteoActuelle = TirerMeteo();
            DistribuerRations();
            AddAudit("AVANCER_JOUR", Data.Campagne.JourActuel.ToString());
            return "✅ Andro " + Data.Campagne.JourActuel + " / Jour " + Data.Campagne.JourActuel;
        }

        public string CreerPoi(string nom, double lat, double lon, bool interdit, string desc)
        {
            if (CurrentUser == null || (CurrentUser.Role != Role.SuperAdmin && CurrentUser.Role != Role.Chef))
                return "❌ Mpitarika na Mpitantana ihany ‖ ❌ Seul Chef ou Super Admin ‖ ❌ Chief or Super Admin only";
            string nomNettoye = (nom ?? "").Trim();
            if (nomNettoye.Length == 0) return "❌ Anarana banga ‖ ❌ Nom vide ‖ ❌ Name empty";
            if (double.IsNaN(lat) || double.IsNaN(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180)
                return "❌ Latitude / Longitude diso ‖ ❌ Latitude / Longitude incorrecte ‖ ❌ Invalid latitude / longitude";

            var poi = new Poi
            {
                Nom = nomNettoye,
                Lat = lat,
                Lon = lon,
                Interdit = interdit,
                Desc = (desc ?? "").Trim(),
                Position3D = Geo.VersMonde(lat, lon)
            };
            Data.Pois.Add(poi);
            AddAudit("CREATE_POI", $"{nomNettoye} interdit={interdit}");
            return "✅ Toerana noforonina / Lieu créé : " + nomNettoye;
        }

        public List<Poi> ZonesInterdites() => Data.Pois.Where(p => p.Interdit).ToList();

        public void SignalerAlerte(string zone, double distanceMetres)
        {
            AddAudit("ALERTE_ZONE", $"{zone} distance={(int)distanceMetres}m");
        }

        public void SignalerAnomalieGps(string motif)
        {
            AddAudit("GPS_ANOMALIE", motif);
        }

        public void EnregistrerSos(string equipe, double lat, double lon)
        {
            Data.DernierSos = new SosEnregistre { Date = DateTime.UtcNow, Equipe = equipe, Lat = lat, Lon = lon };
            AddAudit("SOS", equipe);
        }

        public int CompterAsaRehetraCetteSemaine(string chefPseudo)
        {
            DateTime limite = DateTime.UtcNow.AddDays(-7);
            return Data.Activites.Count(a => a.CreePar == chefPseudo
                && a.Portee == PorteeActivite.Rehetra
                && a.DateCreation >= limite
                && a.Statut != "refusee");
        }

        public string CreerActivite(Activite a, string tarikaCibleId = "", PorteeActivite portee = PorteeActivite.Tarika, bool evenementCommun = false)
        {
            if (CurrentUser == null) return "❌ Midira aloha ‖ ❌ Connecte-toi d'abord ‖ ❌ Log in first";
            Role role = CurrentUser.Role;
            if (role != Role.Animateur && role != Role.Chef && role != Role.SuperAdmin)
                return "❌ Mpanentana, Mpitarika, Mpitantana Ambony ihany ‖ ❌ Seul Animateur, Chef ou Super Admin ‖ ❌ Only Animator, Chief or Super Admin";
            if (string.IsNullOrWhiteSpace(a.Titre)) return "❌ Lohateny banga ‖ ❌ Titre vide ‖ ❌ Title empty";
            if (string.IsNullOrWhiteSpace(a.Clair)) return "❌ Hafatra mazava banga ‖ ❌ Message en clair vide ‖ ❌ Plain message empty";
            if (!CodeEncoder.CodesSupportes.Contains(a.Code)) return "❌ Kaody tsy voaporofo / Code non pris en charge : " + a.Code;
            if (a.Code == "Semaphore" && !a.ActiviteNavale) return "❌ Sémaphore : activité navale obligatoire ‖ ❌ Réservé aux activités navales ‖ ❌ Semaphore: naval activity required";
            if (a.Indices.Count > 3) return "❌ Toro-hevitra 3 farafahakeliny ‖ ❌ 3 indices max ‖ ❌ 3 hints maximum";
            if (a.Points < 1 || a.Points > 100) return "❌ Isa 1 ka hatramin'ny 100 ‖ ❌ Points de 1 à 100 ‖ ❌ Points from 1 to 100";
            if (a.EssaisMax < 1 || a.EssaisMax > EssaisMaxAutorises) return "❌ Andrana 1 ka 3 ‖ ❌ Essais de 1 à 3 ‖ ❌ Attempts from 1 to 3";
            if (!HoraireValide(a.HeureDebut, a.HeureFin)) return "❌ Ora diso (HH:mm, voalohany < farany) ‖ ❌ Horaire invalide (HH:mm, début < fin) ‖ ❌ Invalid time (HH:mm, start < end)";
            int joursMax = Data.Campagne?.Jours ?? MaxJoursCampagne;
            if (a.Jour < 1 || a.Jour > joursMax) return $"❌ Andro 1 ka {joursMax} ‖ ❌ Jour de 1 à {joursMax} ‖ ❌ Day from 1 to {joursMax}";

            a.Id = Guid.NewGuid().ToString("N");
            a.CreePar = CurrentUser.Pseudo;
            a.Portee = portee;
            a.EvenementCommun = evenementCommun;
            a.DateCreation = DateTime.UtcNow;
            a.TexteCode = CodeEncoder.Encode(a.Code, a.Clair, a.Param);
            var reponsesAcceptees = a.Reponses.Count > 0 ? a.Reponses : new List<string> { a.Clair };
            a.EmpreintesReponses = reponsesAcceptees.Select(r => CodeEncoder.Empreinte(a.Id, r, Data.CleHmac)).ToList();
            Poi? poi = Data.Pois.Find(p => p.Nom == a.Lieu);
            if (poi != null) a.Position3D = poi.Position3D;

            if (role != Role.Chef)
            {
                if (portee == PorteeActivite.Rehetra)
                {
                    a.TarikaId = "";
                    a.TarikaNom = "Rehetra / Tous";
                }
                else
                {
                    Tarika? cible = Data.Tarikas.Find(t => t.Id == tarikaCibleId);
                    if (cible == null) return "❌ Fidio ny tarika kendrena ‖ ❌ Choisis le groupe cible ‖ ❌ Choose the target group";
                    a.TarikaId = cible.Id;
                    a.TarikaNom = cible.Nom;
                }
                a.Statut = "brouillon";
                return AjouterActivite(a, "CREATE_ACTIVITE");
            }

            Tarika? monTarika = Data.Tarikas.Find(t => t.Id == CurrentUser.TarikaId);
            if (portee == PorteeActivite.Tarika)
            {
                if (monTarika == null) return "❌ Tsy manana tarika ianao ‖ ❌ Tu n'as pas encore de groupe ‖ ❌ You don't have a group yet";
                a.TarikaId = monTarika.Id;
                a.TarikaNom = monTarika.Nom;
                a.Statut = "publiee";
                return AjouterActivite(a, "CREATE_ACTIVITE_TARIKA");
            }

            if (portee == PorteeActivite.Rehetra)
            {
                if (!evenementCommun) return "❌ Tsy misy hetsika iombonana — fidio 'Asa Tarika' ‖ ❌ Pas d'évènement commun — choisis 'Activité de groupe' ‖ ❌ No common event — choose 'Group activity'";
                int deja = CompterAsaRehetraCetteSemaine(CurrentUser.Pseudo);
                if (deja >= QUOTA_ASA_REHETRA_PAR_SEMAINE)
                    return $"❌ Quota feno — Asa Rehetra {deja}/{QUOTA_ASA_REHETRA_PAR_SEMAINE} tamin'ity herinandro ity ‖ ❌ Quota atteint — Asa Rehetra {deja}/{QUOTA_ASA_REHETRA_PAR_SEMAINE} cette semaine ‖ ❌ Quota reached — common activities {deja}/{QUOTA_ASA_REHETRA_PAR_SEMAINE} this week";
                a.TarikaId = "";
                a.TarikaNom = "Rehetra / Tous";
                a.Statut = "en_attente_validation";
                a.DemandeCrossTarika = true;
                return AjouterActivite(a, "DEMANDE_ASA_REHETRA");
            }

            Tarika? tarikaCross = Data.Tarikas.Find(t => t.Id == tarikaCibleId);
            if (tarikaCross == null) return "❌ Fidio ny tarika kendrena ‖ ❌ Choisis le groupe cible ‖ ❌ Choose the target group";
            if (monTarika != null && tarikaCross.Id == monTarika.Id) return "❌ Io no tarikanao — fidio 'Asa Tarika' ‖ ❌ C'est ton groupe — choisis 'Activité de groupe' ‖ ❌ It's your group — choose 'Group activity'";
            a.TarikaId = tarikaCross.Id;
            a.TarikaNom = tarikaCross.Nom;
            a.Statut = "en_attente_validation";
            a.DemandeCrossTarika = true;
            return AjouterActivite(a, "DEMANDE_CROSS_TARIKA");
        }

        private string AjouterActivite(Activite a, string action)
        {
            Data.Activites.Add(a);
            AddAudit(action, $"{a.Titre} J{a.Jour} {a.Code} {a.Points}pts portee={a.Portee} tarika={a.TarikaNom}");
            return a.Statut switch
            {
                "publiee" => "✅ Asa navoaka / Activité publiée : " + a.Titre + " — " + a.TarikaNom,
                "brouillon" => "✅ Brouillon noforonina / Brouillon créé : " + a.Titre + " — publie-le quand il est prêt",
                _ => "⏳ Fangatahana nalefa — miandry fanamarinana SA ‖ ⏳ Demande envoyée — en attente du Super Admin ‖ ⏳ Request sent — waiting for the Super Admin"
            };
        }

        public string PublierActivite(string activiteId)
        {
            if (CurrentUser == null) return "❌ Midira aloha ‖ ❌ Connecte-toi d'abord ‖ ❌ Log in first";
            Activite? a = Data.Activites.Find(x => x.Id == activiteId);
            if (a == null) return "❌ Asa tsy hita ‖ ❌ Activité introuvable ‖ ❌ Activity not found";
            if (CurrentUser.Role != Role.SuperAdmin && a.CreePar != CurrentUser.Pseudo) return "❌ Tsy azo atao ‖ ❌ Non autorisé ‖ ❌ Not allowed";
            if (a.Statut != "brouillon") return "❌ Tsy brouillon ‖ ❌ Pas un brouillon ‖ ❌ Not a draft";
            a.Statut = "publiee";
            AddAudit("PUBLIER_ACTIVITE", a.Titre);
            return "✅ Asa navoaka / Activité publiée : " + a.Titre;
        }

        public List<Activite> GetActivitesVisibles(User viewer)
        {
            var res = new List<Activite>();
            foreach (Activite a in Data.Activites)
            {
                bool createur = a.CreePar == viewer.Pseudo;

                if (a.Statut == "en_attente_validation" || a.Statut == "refusee")
                {
                    if (viewer.Role == Role.SuperAdmin || createur) res.Add(a);
                    continue;
                }
                if (a.Statut == "brouillon")
                {
                    if (viewer.Role == Role.SuperAdmin || viewer.Role == Role.Animateur || createur) res.Add(a);
                    continue;
                }
                if (viewer.Role == Role.SuperAdmin || viewer.Role == Role.Animateur)
                {
                    res.Add(a);
                    continue;
                }
                if (a.Portee == PorteeActivite.Rehetra)
                {
                    res.Add(a);
                    continue;
                }
                if (!string.IsNullOrEmpty(a.TarikaId) && a.TarikaId == viewer.TarikaId) res.Add(a);
            }
            return res;
        }

        public string ValiderDemandeActivite(string activiteId, bool valider)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany ‖ ❌ Seul Super Admin ‖ ❌ Super Admin only";
            Activite? a = Data.Activites.Find(x => x.Id == activiteId);
            if (a == null) return "❌ Asa tsy hita ‖ ❌ Activité introuvable ‖ ❌ Activity not found";
            if (a.Statut != "en_attente_validation") return "❌ Tsy fangatahana miandry ‖ ❌ Pas une demande en attente ‖ ❌ Not a pending request";

            a.Statut = valider ? "publiee" : "refusee";
            a.DemandeCrossTarika = false;
            AddAudit(valider ? "VALIDER_DEMANDE_ACTIVITE" : "REFUSER_DEMANDE_ACTIVITE", $"{a.Titre} portee={a.Portee} tarika={a.TarikaNom}");
            return valider
                ? "✅ Fangatahana voamarina / Demande validée : " + a.Titre + " — " + a.TarikaNom
                : "❌ Fangatahana nolavina / Demande refusée : " + a.Titre;
        }

        public string SupprimerDemandeRefusee(string activiteId)
        {
            if (CurrentUser == null || CurrentUser.Role != Role.SuperAdmin) return "❌ Mpitantana Ambony ihany";
            Activite? a = Data.Activites.Find(x => x.Id == activiteId);
            if (a == null) return "❌ Asa tsy hita";
            if (a.Statut != "refusee") return "❌ Tsy nolavina ilay asa ‖ ❌ Activité non refusée ‖ ❌ Activity not rejected";
            Data.Activites.Remove(a);
            AddAudit("SUPPRIMER_DEMANDE", a.Titre);
            return "✅ Fangatahana voafafa ‖ ✅ Demande supprimée ‖ ✅ Request deleted";
        }

        public TeamProgress ProgressionCourante()
        {
            string cle = CurrentUser == null ? "" : CleEquipe(CurrentUser);
            TeamProgress? progression = Data.Progressions.Find(p => p.Cle == cle);
            if (progression == null)
            {
                progression = new TeamProgress { Cle = cle };
                Data.Progressions.Add(progression);
            }
            return progression;
        }

        public int ScoreCourant() => ProgressionCourante().Score;

        public Activite? ActiviteActuelle()
        {
            if (CurrentUser == null) return null;
            TeamProgress progression = ProgressionCourante();
            return GetActivitesVisibles(CurrentUser)
                .Where(a => ActiviteJouable(a, progression, out _))
                .OrderBy(a => a.Jour)
                .ThenBy(a => a.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public bool Repondre(Activite a, string reponse, out string retour, out int points)
        {
            retour = "";
            points = 0;
            if (CurrentUser == null)
            {
                retour = "❌ Midira aloha ‖ ❌ Connecte-toi d'abord ‖ ❌ Log in first";
                return false;
            }

            Activite? cible = GetActivitesVisibles(CurrentUser).Find(x => x.Id == a.Id);
            if (cible == null)
            {
                retour = "❌ Asa tsy hita ‖ ❌ Activité introuvable ‖ ❌ Activity not found";
                return false;
            }

            TeamProgress progression = ProgressionCourante();
            if (!ActiviteJouable(cible, progression, out retour)) return false;

            if (CodeEncoder.VerifierReponse(cible, reponse, Data.CleHmac))
            {
                points = cible.Points;
                progression.Score += points;
                progression.Terminees.Add(cible.Id);
                progression.Essais.Remove(cible.Id);
                progression.Verrous.Remove(cible.Id);
                MettreAJourBadges(progression);
                AddAudit("REPONSE_OK", $"{cible.Titre} +{points}pts equipe={progression.Cle}");
                retour = "✅ Marina / Bonne réponse : +" + points + " pts";
                return true;
            }

            int essais = progression.Essais.GetValueOrDefault(cible.Id) + 1;
            progression.Essais[cible.Id] = essais;
            int indexVerrou = Math.Min(essais, DureesVerrouEssaiMinutes.Length) - 1;
            progression.Verrous[cible.Id] = DateTime.UtcNow.AddMinutes(DureesVerrouEssaiMinutes[indexVerrou]);
            bool epuise = essais >= cible.EssaisMax;
            if (epuise)
            {
                progression.Terminees.Add(cible.Id);
                progression.Verrous.Remove(cible.Id);
            }

            string indice = essais <= cible.Indices.Count ? cible.Indices[essais - 1] : "";
            retour = "❌ Diso / Faux (" + essais + "/" + cible.EssaisMax + ")"
                + (indice.Length > 0 ? " — Toro-hevitra / Indice : " + indice : "")
                + (epuise ? " — Vita / Terminé" : "");
            AddAudit("REPONSE_KO", $"{cible.Titre} essai={essais}/{cible.EssaisMax} equipe={progression.Cle}");
            return false;
        }

        private bool ActiviteJouable(Activite a, TeamProgress progression, out string motif)
        {
            motif = "";
            if (a.Statut != "publiee")
            {
                motif = "❌ Tsy navoaka ‖ ❌ Activité non publiée ‖ ❌ Activity not published";
                return false;
            }
            if (Data.Campagne == null || Data.Campagne.Statut != StatutCampagne.Running)
            {
                motif = "⏸️ Tsy mandeha ny lasy ‖ ⏸️ Campagne non démarrée ou en pause ‖ ⏸️ Camp not running or paused";
                return false;
            }
            if (a.Jour > Data.Campagne.JourActuel)
            {
                motif = "🔒 Tsy mbola misokatra / Pas encore débloquée (jour " + a.Jour + ")";
                return false;
            }
            if (!DansFenetreHoraire(a))
            {
                motif = "⏰ Hors horaire / Hors horaire : " + a.HeureDebut + " – " + a.HeureFin;
                return false;
            }
            if (progression.Terminees.Contains(a.Id))
            {
                motif = "✅ Efa vita ‖ ✅ Déjà terminée ‖ ✅ Already completed";
                return false;
            }
            if (progression.Verrous.TryGetValue(a.Id, out DateTime jusquA) && jusquA > DateTime.UtcNow)
            {
                motif = "⏳ Miandry kely ‖ ⏳ Attends un instant ‖ ⏳ Please wait a moment";
                return false;
            }
            return true;
        }

        private static void MettreAJourBadges(TeamProgress progression)
        {
            if (progression.Score >= 50 && !progression.Badges.Contains("Skoto")) progression.Badges.Add("Skoto");
            if (progression.Score >= 100 && !progression.Badges.Contains("Tari-dalana")) progression.Badges.Add("Tari-dalana");
        }

        private static string CleEquipe(User u) =>
            u.Role == Role.Membre ? u.Parrain + "/" + u.Equipe : "chef:" + u.Pseudo;

        private static bool HoraireValide(string debut, string fin) =>
            TimeSpan.TryParseExact(debut ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan d)
            && TimeSpan.TryParseExact(fin ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan f)
            && d < f;

        private static bool DansFenetreHoraire(Activite a)
        {
            if (!HoraireValide(a.HeureDebut, a.HeureFin)) return false;
            TimeSpan debut = TimeSpan.ParseExact(a.HeureDebut, @"hh\:mm", CultureInfo.InvariantCulture);
            TimeSpan fin = TimeSpan.ParseExact(a.HeureFin, @"hh\:mm", CultureInfo.InvariantCulture);
            TimeSpan maintenant = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FuseauCamp()).TimeOfDay;
            return maintenant >= debut && maintenant <= fin;
        }

        private static TimeZoneInfo FuseauCamp()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Indian/Antananarivo");
            }
            catch (Exception)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Antananarivo", TimeSpan.FromHours(3), "Antananarivo", "Antananarivo");
            }
        }
    }
}
