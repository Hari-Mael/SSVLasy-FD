using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sahavola
{
    public partial class Main : Control
    {
        private const string Racine = "RootVBox/ContentScroll/Content/";
        private const string FichierSession = "user://sahavola_session.cfg";
        private const float LargeurLibelle = 240f;
        private const float HauteurChamp = 40f;
        private const string BarreOnglets = "RootVBox/TabScroll/TabBar/";

        private static readonly Dictionary<string, string> Panneaux = new()
        {
            ["Camp"] = "CampPanel",
            ["Perso"] = "PersoPanel",
            ["Tarika"] = "TarikaPanel",
            ["Comptes"] = "ComptesPanel",
            ["Carte"] = "CartePanel",
            ["Activites"] = "ActivitesPanel",
            ["Codes"] = "CodesPanel",
            ["Teknika"] = "TeknikaPanel",
            ["Equipes"] = "EquipesPanel",
            ["Chef"] = "ChefPanel",
            ["Creer"] = "CreerPanel",
            ["Backend"] = "BackendPanel",
            ["3D"] = "Panel3D"
        };

        private static readonly Dictionary<string, string> Onglets = new()
        {
            ["Camp"] = "BtnTabCamp",
            ["Perso"] = "BtnTabPerso",
            ["Tarika"] = "BtnTabTarika",
            ["Comptes"] = "BtnTabComptes",
            ["Carte"] = "BtnTabCarte",
            ["Activites"] = "BtnTabActivites",
            ["Codes"] = "BtnTabCodes",
            ["Teknika"] = "BtnTabTeknika",
            ["Equipes"] = "BtnTabEquipes",
            ["Chef"] = "BtnTabChef",
            ["Creer"] = "BtnTabCreer",
            ["Backend"] = "BtnTabBackend",
            ["3D"] = "BtnTab3D"
        };

        private static readonly string[] Modeles =
        {
            "enigme_codee", "chasse_tresor", "jeu_nuit", "veillee", "survie", "corvee",
            "observation", "secourisme", "grand_jeu", "koja_skoto", "vona", "teknika"
        };

        private GameManager _gm = null!;
        private ResponsiveManager _resp = null!;
        private GPS _gps = null!;
        private CheckBox _chkMemoriser = null!;
        private OptionButton _optMeteo = null!;
        private Label _lblMeteo = null!;
        private Label _lblEnergie = null!;
        private CheckBox _chkNavale = null!;
        private OptionButton _optConsult = null!;
        private Button _btnUtiliser = null!;
        private Button _btnJourSuivant = null!;
        private Button _btnManger = null!;
        private Label _libelleCle = null!;
        private Label _lblEquipe = null!;
        private Button _btnKitay = null!;
        private Button _btnRano = null!;
        private Button _btnSakafo = null!;
        private Button _btnDebloquer = null!;
        private Button _btnRefaire = null!;
        private Button _btnPasser = null!;
        private LineEdit _editRapport = null!;
        private Button _btnRapport = null!;
        private Label _lblRapports = null!;
        private Button _btnMeteo = null!;
        private OptionButton _optLangue = null!;
        private Langue _langue = Langue.Francais;
        private static readonly Dictionary<string, string> AnglaisTraductions = new()
        {
            ["Fidirana / Connexion"] = "Login",
            ["Fidirana"] = "Login",
            ["Connexion"] = "Login",
            ["Mivoaha / Déconnexion"] = "Logout",
            ["Déconnexion"] = "Logout",
            ["Mamorona kaonty / Créer un compte"] = "Create an account",
            ["Mamorona kaonty"] = "Create an account",
            ["Créer un compte"] = "Create an account",
            ["Mangataka famoronana / Demander la création"] = "Request an account",
            ["Mangataka famoronana"] = "Request an account",
            ["Mpitantana Ambony voalohany / Premier Super Admin"] = "First Super Admin",
            ["Mamorona ny Mpitantana Ambony voalohany / Créer le premier Super Admin"] = "Create the first Super Admin",
            ["Lakile fanafenana / Clé de chiffrement — angatahina alohanina / obligatoire d'abord"] = "Encryption key — required first",
            ["Lakile / Clé"] = "Key",
            ["Anarana fidirana / Pseudo"] = "Login name / Username",
            ["Tenimiafina / Mot de passe"] = "Password",
            ["Miditra / Se connecter"] = "Log in",
            ["Tobi / Camp"] = "Camp",
            ["Camp"] = "Camp",
            ["Ny mpilalaoko / Mon joueur"] = "My player",
            ["Mon joueur"] = "My player",
            ["Ny mpilalaoko"] = "My player",
            ["Tarika / Groupe"] = "Team / Group",
            ["Tarika"] = "Team",
            ["Groupe"] = "Group",
            ["Kaonty / Comptes"] = "Accounts",
            ["Kaonty"] = "Accounts",
            ["Comptes"] = "Accounts",
            ["Sarintany / Carte"] = "Map",
            ["Carte"] = "Map",
            ["Asa / Activités"] = "Activities",
            ["Activités"] = "Activities",
            ["Kaody / Codes"] = "Codes",
            ["Codes"] = "Codes",
            ["Teknika Skoto"] = "Skoto Techniques",
            ["Ekipa / Équipes"] = "Teams",
            ["Équipes"] = "Teams",
            ["Mpitarika / Chef"] = "Leader / Chief",
            ["Chef"] = "Chief",
            ["Mamorona / Créer"] = "Create",
            ["Créer"] = "Create",
            ["Rafitra ambadika / Backend"] = "Backend settings",
            ["Backend"] = "Backend",
            ["Fandraisana / Accueil"] = "Home",
            ["Accueil"] = "Home",
            ["GPS"] = "GPS",
            ["Kitapo / Inventaire"] = "Inventory",
            ["Inventaire"] = "Inventory",
            ["Diarin'ny dia / Journal"] = "Journal",
            ["Journal"] = "Journal",
            ["Vonona / Prêt"] = "Ready",
            ["Prêt"] = "Ready",
            ["Enregistrer"] = "Save",
            ["Sauvegarder"] = "Save",
            ["Créer une activité"] = "Create an activity",
            ["Mamorona asa / Créer une activité — Double permission"] = "Create an activity — dual permission",
            ["Activité navale"] = "Naval activity",
            ["Jour suivant"] = "Next day",
            ["Andro manaraka / Jour suivant"] = "Next day",
            ["Nihinana / Manger"] = "Eat",
            ["Manger"] = "Eat",
            ["Mahatsiarova ahy / Se souvenir de moi (pseudo)"] = "Remember me (username)",
            ["Se souvenir de moi"] = "Remember me",
            ["Consulter"] = "Consult",
            ["Hitrandraka / Consulter"] = "Consult",
            ["Utiliser pour une activité"] = "Use for an activity",
            ["Ampiasao amin'ny asa / Utiliser pour une activité"] = "Use for an activity",
            ["Météo"] = "Weather",
            ["Énergie"] = "Energy",
            ["Rations"] = "Rations",
            ["Langue"] = "Language",
        };

        private enum Langue { Francais, Malagasy, Anglais }
        private LineEdit _editCle = null!;
        private VBoxContainer _boiteDemandes = null!;
        private VBoxContainer _boiteActivitesAttente = null!;
        private VBoxContainer _boiteBrouillons = null!;
        private readonly Dictionary<string, Node> _cache = new();
        private readonly List<string> _nomsLieux = new();
        private readonly List<string> _idsTarikasCibles = new();
        private readonly List<Teknika> _teknikas = TeknikaData.All();
        private Activite? _activiteAffichee;
        private bool _stockageOuvert;
        private bool _finMasquee;
        private int _bois;
        private int _feu;
        private bool _nuit;
        private string _backendUrl = "";
        private string _backendCampagne = "";
        private string _backendEquipe = "";
        private string _backendPseudo = "";

        public override void _Ready()
        {
            _gm = GameManager.Instance;
            _resp = ResponsiveManager.Instance;
            _gps = new GPS();
            AddChild(_gps);

            ConstruireChampCle();
            ConstruireMemorisation();
            ConstruireBoites();
            ConstruireEtatCamp();
            ConstruireActiviteNavale();
            ConstruireConsultation();
            ChargerLangue();
            ConstruireLangue();
            RemplirOptions();
            ConnecterBoutons();
            CacherHorsUsage();

            _resp.OnResolutionChangee += AppliquerResponsive;
            AppliquerResponsive(_resp.TypeActuel, _resp.OrientationActuelle, _resp.FacteurEchelle);
            AppliquerSession();
            Callable.From(DiagnostiquerMiseEnPage).CallDeferred();
        }

        public override void _ExitTree()
        {
            if (_resp != null) _resp.OnResolutionChangee -= AppliquerResponsive;
        }

        private T N<T>(string nom) where T : Node
        {
            if (!_cache.TryGetValue(nom, out Node? noeud))
            {
                noeud = FindChild(nom, true, false)
                    ?? throw new InvalidOperationException("Nœud introuvable / Nœud introuvable : " + nom);
                _cache[nom] = noeud;
            }
            return (T)noeud;
        }

        private Control Panneau(string nom) => GetNode<Control>(Racine + Panneaux[nom]);

        private Label Statut() => GetNode<Label>("RootVBox/StatusLabel");

        private void SetStatus(string texte)
        {
            Statut().Text = texte;
            AppliquerLangue();
        }

        private static string Texte(OptionButton option) =>
            option.Selected >= 0 ? option.GetItemText(option.Selected) : "";

        private static void SelectionnerTexte(OptionButton option, string texte)
        {
            for (int i = 0; i < option.ItemCount; i++)
            {
                if (option.GetItemText(i) == texte)
                {
                    option.Select(i);
                    return;
                }
            }
        }

        private static void ViderBoite(Node boite)
        {
            foreach (Node enfant in boite.GetChildren()) enfant.QueueFree();
        }

        private void ConstruireChampCle()
        {
            VBoxContainer authVBox = N<VBoxContainer>("AuthVBox");
            var libelle = new Label
            {
                Text = "Lakile fanafenana / Clé de chiffrement — angatahina alohanina / obligatoire d'abord",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _editCle = new LineEdit
            {
                Secret = true,
                PlaceholderText = "Lakile / Clé",
                CustomMinimumSize = new Vector2(0, 48)
            };
            _libelleCle = libelle;
            authVBox.AddChild(libelle);
            authVBox.AddChild(_editCle);
            authVBox.MoveChild(libelle, 2);
            authVBox.MoveChild(_editCle, 3);
            N<LineEdit>("EditSecret").Visible = false;
            N<Label>("LblSecretInfo").Visible = false;
        }

        private void ConstruireMemorisation()
        {
            VBoxContainer authVBox = N<VBoxContainer>("AuthVBox");
            _chkMemoriser = new CheckBox
            {
                Text = "💾 Mahatsiarova ahy / Se souvenir de moi (pseudo)",
                CustomMinimumSize = new Vector2(0, 40)
            };
            authVBox.AddChild(_chkMemoriser);
            authVBox.MoveChild(_chkMemoriser, N<Button>("BtnLogin").GetIndex());

            var config = new ConfigFile();
            if (config.Load(FichierSession) == Error.Ok)
            {
                string pseudo = (string)config.GetValue("connexion", "pseudo", "");
                N<LineEdit>("EditLoginPseudo").Text = pseudo;
                _chkMemoriser.ButtonPressed = pseudo.Length > 0;
            }
        }

        private void MemoriserPseudo(string pseudo)
        {
            var config = new ConfigFile();
            config.SetValue("connexion", "pseudo", _chkMemoriser.ButtonPressed ? pseudo : "");
            config.Save(FichierSession);
        }

        private void ConstruireEtatCamp()
        {
            Control campVBox = N<Control>("CampVBox");
            _lblMeteo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            _lblEnergie = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            _btnManger = new Button { Text = "🍲 Manger", CustomMinimumSize = new Vector2(0, 44) };
            _btnManger.Pressed += OnManger;
            _optMeteo = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
            foreach (Meteo meteo in Enum.GetValues<Meteo>()) _optMeteo.AddItem(LibelleMeteo(meteo));
            _optMeteo.Select(0);
            _btnMeteo = new Button { Text = "🌦️ Changer la météo", CustomMinimumSize = new Vector2(0, 44) };
            _btnMeteo.Pressed += OnChangerMeteo;
            _btnJourSuivant = new Button { Text = "📅 Jour suivant", CustomMinimumSize = new Vector2(0, 44) };
            _btnJourSuivant.Pressed += OnJourSuivant;
            campVBox.AddChild(_lblMeteo);
            campVBox.AddChild(_lblEnergie);
            campVBox.AddChild(_btnManger);
            campVBox.AddChild(_optMeteo);
            campVBox.AddChild(_btnMeteo);
            campVBox.AddChild(_btnJourSuivant);
            _lblEquipe = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            _btnKitay = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnKitay.Pressed += () => OnCollecter("kitay");
            _btnRano = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnRano.Pressed += () => OnCollecter("rano");
            _btnSakafo = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnSakafo.Pressed += () => OnCollecter("sakafo");
            _btnDebloquer = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnDebloquer.Pressed += OnDebloquer;
            campVBox.AddChild(_lblEquipe);
            campVBox.AddChild(_btnKitay);
            campVBox.AddChild(_btnRano);
            campVBox.AddChild(_btnSakafo);
            campVBox.AddChild(_btnDebloquer);
            _editRapport = new LineEdit { CustomMinimumSize = new Vector2(0, 44) };
            _btnRapport = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnRapport.Pressed += OnRapport;
            _lblRapports = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            campVBox.AddChild(_editRapport);
            campVBox.AddChild(_btnRapport);
            campVBox.AddChild(_lblRapports);
            Control activitesVBox = N<Control>("ActivitesVBox");
            _btnRefaire = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnRefaire.Pressed += OnRefaire;
            _btnPasser = new Button { CustomMinimumSize = new Vector2(0, 44) };
            _btnPasser.Pressed += OnPasser;
            activitesVBox.AddChild(_btnRefaire);
            activitesVBox.AddChild(_btnPasser);
        }

        private void ConstruireActiviteNavale()
        {
            Node formulaire = N<Node>("CreerForm");
            Node parent = formulaire.GetParent();
            var ligne = new HBoxContainer();
            ligne.AddChild(new Label { Text = "⚓ Asa nabo / Activité navale", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            _chkNavale = new CheckBox { Text = "Eny / Oui — obligatoire pour le sémaphore" };
            ligne.AddChild(_chkNavale);
            parent.AddChild(ligne);
            parent.MoveChild(ligne, formulaire.GetIndex() + 1);
        }

        private void OnManger()
        {
            SetStatus(_gm.Manger());
            RafraichirTout();
        }

        private void ChargerLangue()
        {
            var config = new ConfigFile();
            if (config.Load(FichierSession) == Error.Ok)
            {
                _langue = (Langue)(int)config.GetValue("reglages", "langue", 0);
            }
        }

        private void SauverLangue()
        {
            var config = new ConfigFile();
            config.Load(FichierSession);
            config.SetValue("reglages", "langue", (int)_langue);
            config.Save(FichierSession);
        }

        private void ConstruireLangue()
        {
            _optLangue = new OptionButton { CustomMinimumSize = new Vector2(150, 40) };
            _optLangue.AddItem("Français");
            _optLangue.AddItem("Malagasy");
            _optLangue.AddItem("English");
            _optLangue.Select((int)_langue);
            _optLangue.ItemSelected += index => ChangerLangue((Langue)index);
            Node deconnexion = N<Node>("BtnDeconnexion");
            Node barre = deconnexion.GetParent();
            barre.AddChild(_optLangue);
            barre.MoveChild(_optLangue, deconnexion.GetIndex());
        }

        private void ChangerLangue(Langue nouvelle)
        {
            _langue = nouvelle;
            SauverLangue();
            RafraichirTout();
        }

        private string Traduire(string source)
        {
            return string.Join("\n", source.Split('\n').Select(TraduireLigne));
        }

        private string TraduireLigne(string ligne)
        {
            if (string.IsNullOrWhiteSpace(ligne)) return ligne;
            if (ligne.Contains('‖'))
            {
                string[] versions = ligne.Split('‖').Select(v => v.Trim()).ToArray();
                int index = _langue switch
                {
                    Langue.Malagasy => 0,
                    Langue.Anglais => 2,
                    _ => 1
                };
                return versions[Math.Min(index, versions.Length - 1)];
            }
            int separateur = ligne.IndexOf(" / ", StringComparison.Ordinal);
            if (separateur < 0)
            {
                return _langue == Langue.Anglais && AnglaisTraductions.TryGetValue(ligne.Trim(), out string? seule)
                    ? seule
                    : ligne;
            }
            string gauche = ligne[..separateur];
            string droite = ligne[(separateur + 3)..].Trim();
            string prefixe = new string(gauche.TakeWhile(c => !char.IsLetterOrDigit(c)).ToArray());
            string malagasy = gauche[prefixe.Length..].Trim();
            return _langue switch
            {
                Langue.Malagasy => prefixe + malagasy,
                Langue.Anglais => prefixe + (AnglaisTraductions.TryGetValue(droite, out string? anglais) ? anglais : droite),
                _ => prefixe + droite
            };
        }

        private void AppliquerLangue() => AppliquerLangueSur(this);

        private void AppliquerLangueSur(Node noeud)
        {
            foreach (Node enfant in noeud.GetChildren())
            {
                if (enfant is OptionButton option)
                {
                    for (int i = 0; i < option.ItemCount; i++)
                    {
                        int index = i;
                        TraduireMeta(option, "_" + index, option.GetItemText(index), t => option.SetItemText(index, t));
                    }
                }
                else if (enfant is Button bouton)
                {
                    TraduireMeta(bouton, "", bouton.Text, t => bouton.Text = t);
                }
                else if (enfant is Label libelle)
                {
                    TraduireMeta(libelle, "", libelle.Text, t => libelle.Text = t);
                }
                else if (enfant is LineEdit champ)
                {
                    TraduireMeta(champ, "", champ.PlaceholderText, t => champ.PlaceholderText = t);
                }
                AppliquerLangueSur(enfant);
            }
        }

        private void TraduireMeta(Node noeud, string cle, string actuel, Action<string> appliquer)
        {
            string nomSource = "src" + cle;
            string nomSortie = "out" + cle;
            string source = noeud.HasMeta(nomSortie) && (string)noeud.GetMeta(nomSortie) == actuel
                ? (string)noeud.GetMeta(nomSource)
                : actuel;
            string traduit = Traduire(source);
            noeud.SetMeta(nomSource, source);
            noeud.SetMeta(nomSortie, traduit);
            if (traduit != actuel) appliquer(traduit);
        }

        private void OnCollecter(string ressource)
        {
            SetStatus(_gm.Collecter(ressource));
            RafraichirTout();
        }

        private void OnRefaire()
        {
            Activite? a = _gm.ActiviteActuelle();
            if (a == null) return;
            SetStatus(_gm.Refaire(a.Id));
            RafraichirTout();
        }

        private void OnPasser()
        {
            Activite? a = _gm.ActiviteActuelle();
            if (a == null) return;
            SetStatus(_gm.Passer(a.Id));
            RafraichirTout();
        }

        private void OnRapport()
        {
            SetStatus(_gm.EnvoyerRapport(_editRapport.Text));
            _editRapport.Text = "";
            RafraichirTout();
        }

        private void RafraichirBoutonsChoix()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            Activite? a = _gm.ActiviteActuelle();
            TeamProgress p = _gm.ProgressionCourante();
            int essais = a == null ? 0 : p.Essais.GetValueOrDefault(a.Id);
            string choix = a != null && p.Choix.TryGetValue(a.Id, out string? c) ? c ?? "" : "";
            _btnRefaire.Visible = a != null && essais > 0 && choix == "";
            _btnRefaire.Text = T("🔁 Refaire (tsy misy fara andro)", "🔁 Refaire (sans délai)", "🔁 Retry (no delay)");
            _btnPasser.Visible = a != null && essais > 0 && choix != "passer";
            _btnPasser.Text = T("⏭️ Lany — manaraka", "⏭️ Passer à l'étape suivante", "⏭️ Skip to next step");
        }

        private void OnDebloquer()
        {
            SetStatus(_gm.DebloquerEquipes());
            RafraichirTout();
        }

        private void RafraichirVieEquipe()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            TeamProgress p = _gm.ProgressionCourante();
            var cap = _gm.CapaciteStocks();
            string etat = _gm.EtatEquipe(p);
            _lblEquipe.Text = T("🏕️ Ekipa", "🏕️ Équipe", "🏕️ Team") + " — "
                + T("🪵 Kitay", "🪵 Bois", "🪵 Wood") + " " + p.Kitay + "/" + cap.Kitay + " · "
                + T("💧 Rano", "💧 Eau", "💧 Water") + " " + p.Rano + "/" + cap.Rano + " · "
                + T("🍞 Sakafo", "🍞 Nourriture", "🍞 Food") + " " + p.Sakafo + "/" + cap.Sakafo + "\n"
                + LibelleEtatEquipe(etat);

            bool peut = etat == "normal" && _gm.CampagneEnCours();
            _btnKitay.Text = T("🌲 Mitady kitay", "🌲 Ramasser du bois", "🌲 Gather wood") + (p.ActionsDuJour.Contains("kitay") ? " ✅" : "");
            _btnRano.Text = T("💧 Mitady rano", "💧 Chercher de l'eau", "💧 Fetch water") + (p.ActionsDuJour.Contains("rano") ? " ✅" : "");
            _btnSakafo.Text = T("🍞 Mividy sakafo", "🍞 Chercher de la nourriture", "🍞 Get food") + (p.ActionsDuJour.Contains("sakafo") ? " ✅" : "");
            _btnKitay.Disabled = !peut || p.ActionsDuJour.Contains("kitay");
            _btnRano.Disabled = !peut || p.ActionsDuJour.Contains("rano");
            _btnSakafo.Disabled = !peut || p.ActionsDuJour.Contains("sakafo");

            bool chef = u.Role == Role.Chef || u.Role == Role.SuperAdmin;
            _btnDebloquer.Visible = chef;
            _btnDebloquer.Text = T("🔓 Hanafaka ny ekipa", "🔓 Débloquer l'équipe", "🔓 Unblock the team");
            _btnDebloquer.Disabled = etat == "normal";

            bool membre = u.Role == Role.Membre;
            _editRapport.Visible = membre;
            _btnRapport.Visible = membre;
            _btnRapport.Text = T("📝 Tafiray ny rapporta", "📝 Envoyer le rapport du jour", "📝 Send today's report");
            _btnRapport.Disabled = !_gm.RapportOuvert();
            _editRapport.PlaceholderText = T("Rapporta (19h–20h)", "Rapport (19h–20h)", "Report (19:00–20:00)");
            _lblRapports.Visible = u.Role == Role.Chef;
            List<string> manquants = u.Role == Role.Chef ? _gm.RapportsManquants(u.Pseudo) : new List<string>();
            _lblRapports.Text = T("📝 Rapporta tsy voaray", "📝 Rapports manquants", "📝 Missing reports") + " : "
                + (manquants.Count == 0 ? T("tsy misy", "aucun", "none") : string.Join(", ", manquants));
        }

        private string LibelleEtatEquipe(string etat) => etat switch
        {
            "bloque" => T("⏳ Blokao — miandry ny mpitarika", "⏳ Bloquée — en attente du chef", "⏳ Blocked — waiting for the chief"),
            "notif" => T("🔔 Hafatra : mifandraisa amin'ny mpitarika", "🔔 Notification : contactez votre chef pour débloquer", "🔔 Notification: contact your chief to unblock"),
            "malade" => T("🤒 Marary", "🤒 Malade", "🤒 Sick"),
            _ => T("✅ Ekipa mahavita", "✅ Équipe en forme", "✅ Team in good shape")
        };

        private void OnJourSuivant()
        {
            SetStatus(_gm.AvancerJour());
            RafraichirTout();
        }

        private void OnChangerMeteo()
        {
            SetStatus(_gm.ChangerMeteo((Meteo)Math.Max(0, _optMeteo.Selected)));
            RafraichirTout();
        }

        private string T(string malagasy, string francais, string anglais) => _langue switch
        {
            Langue.Malagasy => malagasy,
            Langue.Anglais => anglais,
            _ => francais
        };

        private string LibelleMeteo(Meteo meteo) => meteo switch
        {
            Meteo.Chaleur => T("☀️ Mafana", "☀️ Chaleur", "☀️ Heat"),
            Meteo.Pluvieux => T("🌧️ Orana", "🌧️ Pluvieux", "🌧️ Rainy"),
            Meteo.Venteux => T("💨 Rivotra", "💨 Venteux", "💨 Windy"),
            Meteo.ChaudHumide => T("🥵 Mafana sy mando", "🥵 Chaud et humide", "🥵 Hot and humid"),
            Meteo.VenteuxHumide => T("🌬️ Rivotra sy mando", "🌬️ Venteux et humide", "🌬️ Windy and humid"),
            Meteo.ChaudSec => T("🔥 Mafana sy maina", "🔥 Chaud et sec", "🔥 Hot and dry"),
            _ => ""
        };

        private string LibelleType(TypeLasy type) => type switch
        {
            TypeLasy.Tselatra => T("⚡ Lasy Tselatra", "⚡ Camp Tselatra", "⚡ Tselatra camp"),
            TypeLasy.Lehibe => T("🏕️ Lasy Lehibe", "🏕️ Camp Lehibe", "🏕️ Lehibe camp"),
            TypeLasy.Feno => T("🌲 Lasy Feno", "🌲 Camp Feno", "🌲 Feno camp"),
            _ => ""
        };

        private string LibelleStatut(StatutCampagne statut) => statut switch
        {
            StatutCampagne.Draft => T("Vonona", "Brouillon", "Draft"),
            StatutCampagne.Scheduled => T("Voavaha", "Planifiée", "Scheduled"),
            StatutCampagne.Running => T("Mandeha", "En cours", "Running"),
            StatutCampagne.Paused => T("Atsaharana", "En pause", "Paused"),
            StatutCampagne.Finished => T("Vita", "Terminée", "Finished"),
            _ => ""
        };

        private VBoxContainer BoiteApres(string nomReference)
        {
            Node reference = N<Node>(nomReference);
            Node parent = reference.GetParent();
            var boite = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            parent.AddChild(boite);
            parent.MoveChild(boite, reference.GetIndex() + 1);
            return boite;
        }

        private void ConstruireBoites()
        {
            _boiteDemandes = BoiteApres("LblDemandes");
            _boiteActivitesAttente = BoiteApres("LblDemandesActivitesList");
            _boiteBrouillons = BoiteApres("LblBrouillons");
        }

        private void CacherHorsUsage()
        {
            GetNode<Control>(Racine + "DialogPanel").Visible = false;
            N<Button>("BtnInteract").Visible = false;
            N<Control>("JoueursTitre").Visible = false;
            N<Control>("JoueursForm").Visible = false;
            N<Control>("BtnChoisir").Visible = false;
            N<Control>("LblPlayers").Visible = false;
            N<Button>("BtnPurger").Disabled = true;
            N<Label>("LblTekList").Text = "";
            N<Control>("EndScreen").Visible = false;
        }

        private void RemplirOptions()
        {
            OptionButton regRole = N<OptionButton>("OptRegRole");
            regRole.Clear();
            regRole.AddItem("🧑 Mpikambana / Membre");
            regRole.AddItem("🧭 Mpitarika / Chef");
            regRole.AddItem("🎭 Mpanentana / Animateur");

            OptionButton saRole = N<OptionButton>("OptSaRole");
            saRole.Clear();
            saRole.AddItem("🧭 Mpitarika / Chef");
            saRole.AddItem("🎭 Mpanentana / Animateur");
            saRole.AddItem("👑 Mpitantana Ambony / Super Admin");

            OptionButton poi = N<OptionButton>("OptPoiInterdit");
            poi.Clear();
            poi.AddItem("🟢 Avela / Autorisé");
            poi.AddItem("🔴 Voarara / Interdit (géofence)");

            OptionButton variante = N<OptionButton>("OptNVariante");
            variante.Clear();
            variante.AddItem("An-tany / Terrain");
            variante.AddItem("Virtoaly / Virtuel");
            variante.AddItem("Mifangaro / Hybride");
            variante.Select(2);

            OptionButton portee = N<OptionButton>("OptNPortee");
            portee.Clear();
            portee.AddItem("🏕️ Asa Tarika — Tarikako ihany / Mon groupe — Avy hatrany");
            portee.AddItem("🌍 Asa Rehetra — Tous — Mangata + Super Admin");
            portee.AddItem("🔀 Cross-Tarika — Un autre groupe — Mangata + Super Admin");

            OptionButton campType = N<OptionButton>("OptCampType");
            campType.Clear();
            campType.AddItem("⚡ Lasy Tselatra — 1 → 3 andro / jours");
            campType.AddItem("🏕️ Lasy Lehibe — 1 → 5 andro / jours");
            campType.AddItem("🌲 Lasy Feno — 1 → 11 andro / jours");
            campType.Select(0);

            OptionButton modele = N<OptionButton>("OptNModele");
            modele.Clear();
            foreach (string m in Modeles) modele.AddItem(m);

            OptionButton codeChoix = N<OptionButton>("OptCodeChoix");
            OptionButton codeNouveau = N<OptionButton>("OptNCode");
            codeChoix.Clear();
            codeNouveau.Clear();
            foreach (string code in CodeEncoder.CodesSupportes.OrderBy(c => c))
            {
                codeChoix.AddItem(code);
                codeNouveau.AddItem(code);
            }

            RemplirListe(N<OptionButton>("OptPersoAvatar"), new[] { "🧑", "👩", "🧔", "👱‍♀️", "🦸", "🧙", "🏹", "🗡️", "👑" });
            RemplirListe(N<OptionButton>("OptPersoTenue"), new[] { "Tenue Skoto Classique", "Tenue Nuit", "Tenue Pluie", "Tenue Cérémonie", "Explorateur" });
            RemplirListe(N<OptionButton>("OptPersoFoulard"), new[] { "Rouge", "Vert", "Bleu", "Jaune", "Orange" });
            RemplirListe(N<OptionButton>("OptPersoArme"), new[] { "Bâton", "Corde", "Lampe", "Boussole", "Sac" });
            RemplirListe(N<OptionButton>("OptPersoGenre"), new[] { "Non précisé", "Garçon", "Fille" });
            RemplirListe(N<OptionButton>("OptPersoCheveux"), new[] { "Court", "Long", "Tressé", "Rasé", "Natte" });
            RemplirListe(N<OptionButton>("OptTarikaEmbleme"), new[] { "🦅", "🦁", "🐢", "🐘", "🌳", "🔥", "⭐", "🏔️" });
        }

        private static void RemplirListe(OptionButton option, IEnumerable<string> elements)
        {
            option.Clear();
            foreach (string e in elements) option.AddItem(e);
            if (option.ItemCount > 0) option.Select(0);
        }

        private void ConnecterBoutons()
        {
            N<Button>("BtnAuthLogin").Pressed += () => ChoisirAuth("login");
            N<Button>("BtnAuthRegister").Pressed += () => ChoisirAuth("register");
            N<Button>("BtnAuthFirst").Pressed += () => ChoisirAuth("first");
            N<Button>("BtnLogin").Pressed += OnConnexion;
            N<Button>("BtnRegister").Pressed += OnDemandeCreation;
            N<Button>("BtnFirst").Pressed += OnPremierSuperAdmin;

            N<Button>("BtnDeconnexion").Pressed += OnDeconnexion;
            N<Button>("BtnLogout").Pressed += OnDeconnexion;

            foreach (KeyValuePair<string, string> onglet in Onglets)
            {
                string panneau = onglet.Key;
                N<Button>(onglet.Value).Pressed += () => ChoisirPanneau(panneau);
            }

            N<Button>("BtnBGps").Pressed += OnGps;
            N<Button>("BtnDialogOk").Pressed += () => GetNode<Control>(Racine + "DialogPanel").Visible = false;
            N<Button>("BtnOuvrir3D").Pressed += () => AfficherDialogue(
                T("🗺️ Camp 3D", "🗺️ Camp 3D", "🗺️ 3D camp"),
                T("Bientôt disponible : la vue 3D du camp n'est pas encore construite.",
                  "Bientôt disponible : la vue 3D du camp n'est pas encore construite.",
                  "Coming soon: the 3D camp view is not built yet."));
            N<Button>("BtnQuestHelp").Pressed += () => AfficherDialogue(
                T("❓ Aide", "❓ Aide", "❓ Help"),
                T("Une fois par jour, collectez bois, eau et nourriture. Répondez aux activités débloquées. Après une erreur, attendez le délai ou choisissez Refaire ou Passer.",
                  "Une fois par jour, collectez bois, eau et nourriture. Répondez aux activités débloquées. Après une erreur, attendez le délai ou choisissez Refaire ou Passer.",
                  "Once a day, collect wood, water and food. Answer the unlocked activities. After a wrong answer, wait for the delay or choose Retry or Skip."));
            N<Button>("BtnBInv").Pressed += () => ChoisirPanneau("Camp");
            N<Button>("BtnBJournal").Pressed += () => ChoisirPanneau("Activites");
            N<Button>("BtnBCarte").Pressed += () => ChoisirPanneau("Carte");
            N<Button>("BtnBCodes").Pressed += () => ChoisirPanneau("Codes");
            N<Button>("BtnBSos").Pressed += OnSos;
            N<Button>("BtnBHome").Pressed += () => ChoisirPanneau("Camp");

            N<Button>("BtnFeu").Pressed += OnBois;
            N<Button>("BtnNuit").Pressed += OnBasculerNuit;
            N<Button>("BtnGps").Pressed += OnGps;
            N<Button>("BtnSos").Pressed += OnSos;
            N<Button>("BtnCampCreer").Pressed += OnCampagneCreer;
            N<Button>("BtnCampPause").Pressed += () => OnCampagneEtat(StatutCampagne.Paused);
            N<Button>("BtnCampReprendre").Pressed += () => OnCampagneEtat(StatutCampagne.Running);
            N<Button>("BtnCampFin").Pressed += () => OnCampagneEtat(StatutCampagne.Finished);

            N<Button>("BtnPersoSauver").Pressed += OnPersoSauver;
            N<Button>("BtnTarikaCreer").Pressed += OnTarikaCreer;
            N<Button>("BtnSuperCreer").Pressed += OnCreerCompteSuperAdmin;
            N<Button>("BtnPoiCreer").Pressed += OnPoiCreer;
            N<Button>("BtnRepondre").Pressed += OnRepondre;
            N<Button>("BtnEncoder").Pressed += OnEncoder;
            N<Button>("BtnQuiz").Pressed += OnConsulter;
            N<LineEdit>("EditTekSearch").TextChanged += texte => RafraichirTeknika();
            N<Button>("BtnTestAlerte").Pressed += OnTestAlerte;
            N<Button>("BtnVueAerienne").Pressed += () => ChoisirPanneau("Carte");
            N<Button>("BtnVoirAudit").Pressed += RafraichirAudit;
            N<Button>("BtnCreerAct").Pressed += OnCreerActivite;
            N<LineEdit>("EditNClair").TextChanged += texte => RafraichirApercu();
            N<OptionButton>("OptNCode").ItemSelected += index => RafraichirApercu();
            N<Button>("BtnSaveBackend").Pressed += OnSauverBackend;
            N<Button>("BtnTestBackend").Pressed += OnTesterBackend;
            N<Button>("BtnLoginBackend").Pressed += () => Statut().Text = "Fidirana backend tsy mbola misy / Connexion backend non disponible";
            N<Button>("BtnRejouer").Pressed += () => { _finMasquee = true; N<Control>("EndScreen").Visible = false; };
        }

        private void AppliquerResponsive(TypeEcran type, OrientationEcran orientation, float echelle)
        {
            AdapterArbre(this);
            Callable.From(CorrigerMiseEnPage).CallDeferred();
        }

        private void CorrigerMiseEnPage()
        {
            foreach (string nom in new[] { "TitreLabel", "StatusMini" })
            {
                Label libelle = N<Label>(nom);
                libelle.AutowrapMode = TextServer.AutowrapMode.Off;
                libelle.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            }
            N<Label>("StatusLabel").SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            N<HBoxContainer>("TopBar").SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            N<HBoxContainer>("BottomBar").SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            GetNode<ScrollContainer>("RootVBox/ContentScroll").HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
            N<Control>("Content").SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            StabiliserFormulaires(N<Control>("Content"));
            N<SpinBox>("EditNEssais").MaxValue = 3;
            GetNode<ScrollContainer>("RootVBox/ContentScroll").ScrollHorizontal = 0;
        }

        private void StabiliserFormulaires(Node noeud)
        {
            foreach (Node enfant in noeud.GetChildren())
            {
                if (enfant is GridContainer grille)
                {
                    foreach (Node element in grille.GetChildren())
                    {
                        if (element is Label libelle)
                        {
                            libelle.CustomMinimumSize = new Vector2(LargeurLibelle, 0);
                            libelle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                        }
                        else if (element is Control controle)
                        {
                            controle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                            if (controle is LineEdit or OptionButton or SpinBox)
                            {
                                controle.CustomMinimumSize = new Vector2(0, HauteurChamp);
                            }
                        }
                    }
                }
                StabiliserFormulaires(enfant);
            }
        }

        private void DiagnostiquerMiseEnPage()
        {
            GD.Print("[Diag] fenêtre=" + GetViewportRect().Size);
            foreach (string nom in new[] { "TopBar", "ContentScroll", "Content", "AuthPanel", "LoginForm", "BtnLogin" })
            {
                Control c = N<Control>(nom);
                GD.Print("[Diag] " + nom + " pos=" + c.GlobalPosition + " taille=" + c.Size + " visible=" + c.IsVisibleInTree());
            }
            foreach (Node enfant in N<GridContainer>("LoginForm").GetChildren())
            {
                if (enfant is Control ctl)
                {
                    GD.Print("[Diag] LoginForm/" + ctl.Name + " taille=" + ctl.Size + " visible=" + ctl.IsVisibleInTree());
                }
            }
        }

        private void AdapterArbre(Node noeud)
        {
            foreach (Node enfant in noeud.GetChildren())
            {
                if (enfant is Button bouton) _resp.AdapterBouton(bouton);
                else if (enfant is Label libelle) _resp.AdapterLabel(libelle);
                AdapterArbre(enfant);
            }
        }

        private bool OuvrirStockage()
        {
            if (_stockageOuvert) return true;
            string cle = _editCle.Text;
            if (string.IsNullOrEmpty(cle))
            {
                N<Label>("LblAuthMsg").Text = "❌ Angatahina ny lakile / Clé obligatoire";
                return false;
            }
            _gm.SetSecret(cle);
            _editCle.Text = "";
            if (_gm.LectureSeule)
            {
                N<Label>("LblAuthMsg").Text = "❌ " + _gm.ErreurStockage;
                return false;
            }
            _stockageOuvert = true;
            ChoisirAuth(_gm.Data.Users.Count == 0 ? "first" : "login");
            return true;
        }

        private void ChoisirAuth(string mode)
        {
            N<GridContainer>("LoginForm").Visible = mode == "login";
            N<Button>("BtnLogin").Visible = mode == "login";
            N<Label>("LblLoginHint").Visible = mode == "login";
            N<GridContainer>("RegisterForm").Visible = mode == "register";
            N<Button>("BtnRegister").Visible = mode == "register";
            N<GridContainer>("FirstForm").Visible = mode == "first";
            N<Button>("BtnFirst").Visible = mode == "first";
            N<Label>("LblAuthMsg").Text = "";
        }

        private void OnConnexion()
        {
            if (!OuvrirStockage()) return;
            string message = _gm.Login(N<LineEdit>("EditLoginPseudo").Text, N<LineEdit>("EditLoginPass").Text);
            N<Label>("LblAuthMsg").Text = message;
            if (_gm.CurrentUser == null) return;
            MemoriserPseudo(N<LineEdit>("EditLoginPseudo").Text.Trim());
            N<LineEdit>("EditLoginPass").Text = "";
            AppliquerSession();
        }

        private void OnDemandeCreation()
        {
            if (!OuvrirStockage()) return;
            Role role = N<OptionButton>("OptRegRole").Selected switch
            {
                1 => Role.Chef,
                2 => Role.Animateur,
                _ => Role.Membre
            };
            N<Label>("LblAuthMsg").Text = _gm.CreateUser(
                N<LineEdit>("EditRegPseudo").Text,
                N<LineEdit>("EditRegPass").Text,
                role,
                N<LineEdit>("EditRegEquipe").Text,
                N<LineEdit>("EditRegRoleEq").Text,
                N<LineEdit>("EditRegParrain").Text,
                false,
                N<CheckBox>("CheckConsentement").ButtonPressed,
                N<LineEdit>("EditRegParent").Text,
                N<LineEdit>("EditRegContactParent").Text);
        }

        private void OnPremierSuperAdmin()
        {
            if (!OuvrirStockage()) return;
            N<Label>("LblAuthMsg").Text = _gm.CreateFirstSuperAdmin(N<LineEdit>("EditFirstPseudo").Text, N<LineEdit>("EditFirstPass").Text);
            if (_gm.Data.Users.Count > 0) ChoisirAuth("login");
        }

        private void OnDeconnexion()
        {
            _gm.Logout();
            _activiteAffichee = null;
            _finMasquee = false;
            AppliquerSession();
        }

        private void AppliquerSession()
        {
            User? u = _gm.CurrentUser;
            bool connecte = u != null;

            GetNode<Control>(Racine + "AuthPanel").Visible = !connecte;
            GetNode<Control>("RootVBox/TabScroll").Visible = connecte;
            GetNode<Control>("RootVBox/BottomBar").Visible = connecte;
            N<Button>("BtnDeconnexion").Visible = connecte;
            N<Label>("StatusMini").Text = connecte ? u!.Pseudo + " — " + u.Role : "Vonona / Prêt";

            foreach (string nom in Panneaux.Keys) Panneau(nom).Visible = false;
            if (!connecte)
            {
                if (!_stockageOuvert) ChoisirAuth("login");
                RafraichirTout();
                return;
            }

            foreach (KeyValuePair<string, string> onglet in Onglets)
            {
                N<Button>(onglet.Value).Visible = AutorisePanneau(onglet.Key);
            }
            ChargerPerso();
            ChoisirPanneau("Camp");
        }

        private bool AutorisePanneau(string nom)
        {
            Role? role = _gm.CurrentUser?.Role;
            if (role == null) return false;
            bool sa = role == Role.SuperAdmin;
            bool chef = role == Role.Chef;
            bool animateur = role == Role.Animateur;
            return nom switch
            {
                "Tarika" or "Backend" => sa,
                "Comptes" or "Carte" or "Equipes" or "Chef" => sa || chef,
                "Creer" => sa || chef || animateur,
                _ => true
            };
        }

        private void ChoisirPanneau(string nom)
        {
            if (_gm.CurrentUser == null) return;
            if (!AutorisePanneau(nom))
            {
                SetStatus("❌ Tsy azo ny miditra / Accès refusé");
                return;
            }
            foreach (string cle in Panneaux.Keys) Panneau(cle).Visible = cle == nom;
            RafraichirTout();
        }

        private void RafraichirTout()
        {
            RafraichirEntete();
            RafraichirCamp();
            RafraichirComptes();
            RafraichirTarika();
            RafraichirCarte();
            RafraichirActivites();
            RafraichirTeknika();
            RafraichirEquipes();
            RafraichirChef();
            RafraichirCreer();
            RafraichirBackend();
            TexteNavigation();
            TexteMonJoueurActivitesCodes();
            RafraichirBoutonsChoix();
            TexteTarikaCarteEquipesBackend();
            TextePanel3D();
            TexteComptesChefCreer();
            AppliquerLangue();
        }

        private void RafraichirEntete()
        {
            Statut().Text = _gm.ErreurStockage ?? T("Prêt", "Prêt", "Ready");
            N<Label>("StatusMini").Text = _gm.CurrentUser == null
                ? T("Vonona", "Prêt", "Ready")
                : _gm.CurrentUser.Pseudo + " — " + LibelleRole(_gm.CurrentUser.Role);
        }

        private string LibelleRole(Role role) => role switch
        {
            Role.Membre => T("Mpikambana", "Membre", "Member"),
            Role.Chef => T("Mpitarika", "Chef", "Chief"),
            Role.Animateur => T("Mpanentana", "Animateur", "Animator"),
            Role.SuperAdmin => T("Mpitantana Ambony", "Super Admin", "Super Admin"),
            _ => ""
        };

        private void TexteMonJoueurActivitesCodes()
        {
            N<Label>("PersoTitre").Text = T("🎮 Ny mpilalaoko", "🎮 Mon joueur", "🎮 My player");
            N<Label>("LblPersoNom").Text = T("Anarana ny mpilalao", "Nom du personnage", "Character name");
            N<LineEdit>("EditPersoNom").PlaceholderText = T("Anarana…", "Nom…", "Name…");
            N<Label>("LblPersoAvatar").Text = T("Avatar / Emoji", "Avatar / Emoji", "Avatar / Emoji");
            N<Label>("LblPersoTenue").Text = T("Akanjo", "Tenue", "Outfit");
            N<Label>("LblPersoFoulard").Text = T("Foulard — loko", "Foulard — couleur", "Scarf — colour");
            N<Label>("LblPersoArme").Text = T("Fitaovana", "Équipement", "Equipment");
            N<Label>("LblPersoTaille").Text = T("Habe (1.50 — 2.00 m)", "Taille (1.50 — 2.00 m)", "Height (1.50 — 2.00 m)");
            N<Label>("LblPersoGenre").Text = T("Lahy/vavy", "Genre", "Gender");
            N<Label>("LblPersoCheveux").Text = T("Volo", "Cheveux", "Hair");
            N<Button>("BtnPersoSauver").Text = T("💾 Tehirizo ny mpilalaoko", "💾 Sauvegarder mon joueur", "💾 Save my player");

            N<Label>("JouerTitre").Text = T("▶️ Milalao", "▶️ Jouer", "▶️ Play");
            N<Button>("BtnRepondre").Text = T("✅ Hamarino (HMAC)", "✅ Valider (HMAC)", "✅ Validate (HMAC)");
            N<Label>("CodesTitre").Text = T("🔐 Kaody — Teknika Skoto A, p.1–30", "🔐 Codes — Teknika Skoto A, p.1–30", "🔐 Codes — Teknika Skoto A, p.1–30");
            N<Label>("CodesNote").Text = T("Ny kaody no miaro ny tsiambaratelo, mila mamadika azy.", "Le code protège le secret, il faut le décrypter.", "The code protects the secret; it must be decoded.");
            N<Label>("LblMsgClair").Text = T("Hafatra mazava", "Message en clair", "Plain message");
            N<LineEdit>("EditMsgClair").PlaceholderText = T("Hafatra mazava", "Message en clair", "Plain message");
            N<Label>("LblCodeChoix").Text = T("Kaody", "Code", "Code");
            N<Label>("LblParam").Text = T("Fepetra : elanelana na teny fanalahidy", "Décalage : nombre ou mot-clé", "Shift: number or keyword");
            N<LineEdit>("EditParam").PlaceholderText = T("3 na CLE", "3 ou CLE", "3 or KEY");
            N<Button>("BtnEncoder").Text = T("🔐 Kaody — Hanoratra", "🔐 Encoder", "🔐 Encode");
        }

        private void TexteTarikaCarteEquipesBackend()
        {
            N<Label>("TarikaTitre").Text = T("🏕️ Tarika / Groupe", "🏕️ Groupe", "🏕️ Group");
            N<Label>("LblTarikaNom").Text = T("Anaran'ny tarika", "Nom du groupe", "Group name");
            N<LineEdit>("EditTarikaNom").PlaceholderText = T("Tarika…", "Nom du groupe…", "Group name…");
            N<Label>("LblTarikaChef").Text = T("Mpitarika", "Chef", "Leader");
            N<Label>("LblTarikaEmbleme").Text = T("Marika", "Emblème", "Emblem");
            N<Label>("LblTarikaDevise").Text = T("Teny filamatra", "Devise", "Motto");
            N<LineEdit>("EditTarikaDevise").PlaceholderText = T("Vonona foana !", "Toujours prêt !", "Always ready!");
            N<Button>("BtnTarikaCreer").Text = T("➕ Mamorona tarika", "➕ Créer un groupe", "➕ Create a group");

            N<Label>("CarteTitre").Text = T("🗺️ Sarintany 2D — Toerana noforoninao", "🗺️ Carte 2D — Lieux créés par toi", "🗺️ 2D map — places you created");
            N<Label>("CarteInfo").Text = T("Sarintany 2D — tsindrio ny toerana", "Carte 2D — clique sur un lieu", "2D map — click a place");
            N<Label>("LblPoiNom").Text = T("Anarana", "Nom", "Name");
            N<LineEdit>("EditPoiNom").PlaceholderText = T("Anarana", "Nom", "Name");
            N<Label>("LblPoiInterdit").Text = T("Faritra", "Zone", "Zone");
            N<Label>("LblPoiDesc").Text = T("Famaritana", "Description", "Description");
            N<LineEdit>("EditPoiDesc").PlaceholderText = T("Famaritana", "Description", "Description");
            N<Button>("BtnPoiCreer").Text = T("➕ Mamorona toerana", "➕ Créer un lieu", "➕ Create a place");

            N<Label>("EquipesTitre").Text = T("👥 Ekipa", "👥 Équipes", "👥 Teams");
            N<Label>("EquipesNote").Text = T("Isaky ny mpitarika : ekipa 3 farafahabetsany, mpikambana 5 isaky ny ekipa.",
                "Par chef : 3 équipes maximum, 5 membres par équipe.",
                "Per leader: 3 teams maximum, 5 members per team.");

            N<Label>("BackendTitre").Text = T("⚙️ Rafitra ambadika", "⚙️ Backend", "⚙️ Backend");
            N<Label>("LblBackendUrl").Text = "URL Backend";
            N<Label>("LblEquipeId").Text = T("Ekipa", "Équipe", "Team");
            N<LineEdit>("EditEquipeId").PlaceholderText = T("Ekipa", "Équipe", "Team");
            N<Label>("LblPseudoBackend").Text = T("Pseudo", "Pseudo", "Username");
            N<LineEdit>("EditPseudoBackend").PlaceholderText = T("Pseudo", "Pseudo", "Username");
            N<Button>("BtnSaveBackend").Text = T("💾 Tehirizo", "💾 Sauvegarder", "💾 Save");
            N<Button>("BtnLoginBackend").Text = T("🔐 Fidirana", "🔐 Connexion", "🔐 Login");
            N<Button>("BtnTestBackend").Text = T("🧪 Fitsapana", "🧪 Tester", "🧪 Test");
        }

        private void TexteComptesChefCreer()
        {
            N<Label>("ComptesTitre").Text = T("👑 Kaonty", "👑 Comptes", "👑 Accounts");
            N<Label>("LblSuper").Text = T("👑 Mpitantana Ambony — mitantana ny zava-drehetra", "👑 Super Admin — gère tout", "👑 Super Admin — manages everything");
            N<Label>("LblSaPseudo").Text = T("Pseudo", "Pseudo", "Username");
            N<LineEdit>("EditSaPseudo").PlaceholderText = T("Pseudo", "Pseudo", "Username");
            N<Label>("LblSaPass").Text = T("Tenimiafina", "Mot de passe", "Password");
            N<LineEdit>("EditSaPass").PlaceholderText = T("Tenimiafina", "Mot de passe", "Password");
            N<Label>("LblSaRole").Text = T("Andraikitra", "Rôle", "Role");
            N<Label>("LblSaEquipe").Text = T("Ekipa", "Équipe", "Team");
            N<LineEdit>("EditSaEquipe").PlaceholderText = T("Ekipa", "Équipe", "Team");
            N<Button>("BtnSuperCreer").Text = T("👑 Mamorona sy manamarina", "👑 Créer et valider", "👑 Create and validate");
            N<Label>("LblChefPseudo").Text = T("Pseudo", "Pseudo", "Username");
            N<LineEdit>("EditChefPseudo").PlaceholderText = T("Pseudo", "Pseudo", "Username");
            N<Label>("LblChefPass").Text = T("Tenimiafina", "Mot de passe", "Password");
            N<LineEdit>("EditChefPass").PlaceholderText = T("Tenimiafina", "Mot de passe", "Password");
            N<Button>("BtnLogout").Text = T("🚪 Mivoaha", "🚪 Déconnexion", "🚪 Log out");

            OptionButton roles = N<OptionButton>("OptSaRole");
            roles.SetItemText(0, T("🧭 Mpitarika", "🧭 Chef", "🧭 Leader"));
            roles.SetItemText(1, T("🎭 Mpanentana", "🎭 Animateur", "🎭 Animator"));
            roles.SetItemText(2, T("👑 Mpitantana Ambony", "👑 Super Admin", "👑 Super Admin"));

            N<Label>("ChefTitre").Text = T("🧭 Toeran'ny mpitarika", "🧭 Poste Chef", "🧭 Leader's post");
            N<Label>("AlertesTitre").Text = T("🚨 Fampitandremana", "🚨 Alertes", "🚨 Alerts");
            N<Button>("BtnTestAlerte").Text = T("🧪 Fitsapana", "🧪 Tester", "🧪 Test");
            N<Button>("BtnPurger").Text = T("🧹 Fafao", "🧹 Purger", "🧹 Purge");
            N<Button>("BtnVueAerienne").Text = T("🎮 Fijerena avy eny ambony", "🎮 Vue aérienne", "🎮 Aerial view");
            N<Label>("AuditTitre").Text = T("📋 Audit — iza no nanao inona ?", "📋 Audit — qui a fait quoi ?", "📋 Audit — who did what?");
            N<Button>("BtnVoirAudit").Text = T("📋 Jereo ny audit", "📋 Voir l'audit", "📋 View audit");

            N<Label>("CreerTitre").Text = T("➕ Mamorona asa — fahazoan-dalana roa", "➕ Créer une activité — double permission", "➕ Create an activity — dual permission");
            N<Label>("LblNTitre").Text = T("Lohateny", "Titre", "Title");
            N<LineEdit>("EditNTitre").PlaceholderText = T("Lohateny", "Titre", "Title");
            N<Label>("LblNModele").Text = T("Modely", "Modèle", "Model");
            N<Label>("LblNCode").Text = T("Kaody", "Code", "Code");
            N<Label>("LblNClair").Text = T("Hafatra mazava", "Message en clair", "Plain message");
            N<LineEdit>("EditNClair").PlaceholderText = T("Hafatra mazava", "Message en clair", "Plain message");
            N<Label>("LblNPoints").Text = T("Isa", "Points", "Points");
            N<Label>("LblNJour").Text = T("Andro (1–11) — araka ny sokajin'ny lasy", "Jour (1–11) — selon le type de camp", "Day (1–11) — depends on camp type");
            N<Label>("LblNHeure").Text = T("Ora", "Horaire", "Time");
            N<Label>("LblNLieu").Text = T("Toerana", "Lieu", "Place");
            N<Label>("LblNTeknika").Text = T("Teknika", "Technique", "Technique");
            N<LineEdit>("EditNTeknika").PlaceholderText = T("Vona Seza", "Nœud de chaise", "Chair knot");
            N<Label>("LblNReponses").Text = T("Valiny ekena (sarahina amin'ny ;)", "Réponses acceptées (séparées par ;)", "Accepted answers (separated by ;)");
            N<Label>("LblNIndices").Text = T("Toro-hevitra (3 farafahakeliny)", "Indices (3 maximum)", "Hints (3 maximum)");
            N<Label>("LblNEssais").Text = T("Isan'ny andrana", "Nombre d'essais", "Number of attempts");
            N<Label>("LblNVariante").Text = T("Karazana", "Variante", "Variant");
            N<Label>("LblNPortee").Text = T("Ho an'iza ?", "Pour qui ?", "For whom?");
            N<Label>("LblNTarikaCible").Text = T("Tarika kendrena (raha Cross-Tarika)", "Groupe cible (si cross-groupe)", "Target group (if cross-group)");
            N<Label>("LblNEvenement").Text = T("Hetsika iombonana ?", "Évènement commun ?", "Common event?");
            N<CheckBox>("CheckNEvenementCommun").Text = T("✅ Hetsika iombonana", "✅ Évènement commun", "✅ Common event");
            N<Button>("BtnCreerAct").Text = T("✅ Mamorona volavola", "✅ Créer un brouillon", "✅ Create a draft");

            OptionButton variantes = N<OptionButton>("OptNVariante");
            variantes.SetItemText(0, T("An-tany", "Terrain", "Field"));
            variantes.SetItemText(1, T("Virtoaly", "Virtuel", "Virtual"));
            variantes.SetItemText(2, T("Mifangaro", "Hybride", "Hybrid"));
            OptionButton portees = N<OptionButton>("OptNPortee");
            portees.SetItemText(0, T("🏕️ Asa tarika — tarikako ihany", "🏕️ Activité de groupe — mon groupe", "🏕️ Group activity — my group"));
            portees.SetItemText(1, T("🌍 Asa rehetra — mangata + SA", "🌍 Activité commune — validation SA", "🌍 Common activity — SA validation"));
            portees.SetItemText(2, T("🔀 Cross-tarika — tarika hafa", "🔀 Cross-groupe — autre groupe", "🔀 Cross-group — another group"));
            OptionButton poi = N<OptionButton>("OptPoiInterdit");
            poi.SetItemText(0, T("🟢 Avela", "🟢 Autorisé", "🟢 Allowed"));
            poi.SetItemText(1, T("🔴 Voarara (géofence)", "🔴 Interdit (géofence)", "🔴 Forbidden (geofence)"));
        }

        private string Pick(string triple)
        {
            string[] versions = triple.Split('‖').Select(v => v.Trim()).ToArray();
            int index = _langue switch
            {
                Langue.Malagasy => 0,
                Langue.Anglais => 2,
                _ => 1
            };
            return versions[Math.Min(index, versions.Length - 1)];
        }

        private static string SansBarre(string texte) => texte.Replace(" / ", " ; ");

        private void RafraichirOptionsTeknika()
        {
            int selection = _optConsult.Selected;
            _optConsult.Clear();
            foreach (Teknika t in _teknikas)
            {
                _optConsult.AddItem(Pick(t.Categorie) + " — " + Pick(t.Titre) + " — " + t.Page);
            }
            if (_optConsult.ItemCount > 0) _optConsult.Select(Math.Max(0, selection));
        }

        private void AfficherDialogue(string nom, string texte)
        {
            N<Label>("LblDialogNom").Text = nom;
            N<Label>("LblDialogTxt").Text = texte;
            GetNode<Control>(Racine + "DialogPanel").Visible = true;
        }

        private void TextePanel3D()
        {
            N<Label>("Titre3D").Text = T("🗺️ Camp 3D", "🗺️ Camp 3D", "🗺️ 3D camp");
            N<Label>("Info3D").Text = T("Vue du camp en trois dimensions : en construction.", "Vue du camp en trois dimensions : en construction.", "3D view of the camp: under construction.");
            N<Button>("BtnOuvrir3D").Text = T("🗺️ Ouvrir la vue 3D", "🗺️ Ouvrir la vue 3D", "🗺️ Open 3D view");
            N<Label>("LblQuest").Text = T("🧭 Missions", "🧭 Missions", "🧭 Missions");
            N<Label>("LblQuestList").Text = T("Aucune mission pour l'instant.", "Aucune mission pour l'instant.", "No mission for now.");
            N<Button>("BtnQuestHelp").Text = T("❓ Aide", "❓ Aide", "❓ Help");
        }

        private void TexteNavigation()
        {
            N<Label>("TitreLabel").Text = T("🌲 SAHAVOLA — Lasy Skoto", "🌲 SAHAVOLA — Camp Scout", "🌲 SAHAVOLA — Scout Camp");
            N<Button>("BtnDeconnexion").Text = T("🚪 Mivoaha", "🚪 Déconnexion", "🚪 Log out");

            N<Button>("BtnTabCamp").Text = T("🏕️ Tobi", "🏕️ Camp", "🏕️ Camp");
            N<Button>("BtnTabPerso").Text = T("🎮 Ny mpilalaoko", "🎮 Mon joueur", "🎮 My player");
            N<Button>("BtnTabTarika").Text = T("⛺ Tarika", "⛺ Groupe", "⛺ Team");
            N<Button>("BtnTabComptes").Text = T("👑 Kaonty", "👑 Comptes", "👑 Accounts");
            N<Button>("BtnTabCarte").Text = T("🗺️ Sarintany", "🗺️ Carte", "🗺️ Map");
            N<Button>("BtnTabActivites").Text = T("🎯 Asa", "🎯 Activités", "🎯 Activities");
            N<Button>("BtnTabCodes").Text = T("🔐 Kaody", "🔐 Codes", "🔐 Codes");
            N<Button>("BtnTabTeknika").Text = T("📖 Teknika Skoto", "📖 Teknika Skoto", "📖 Teknika Skoto");
            N<Button>("BtnTabEquipes").Text = T("👥 Ekipa", "👥 Équipes", "👥 Teams");
            N<Button>("BtnTabChef").Text = T("🧭 Mpitarika", "🧭 Chef", "🧭 Leader");
            N<Button>("BtnTabCreer").Text = T("➕ Mamorona", "➕ Créer", "➕ Create");
            N<Button>("BtnTabBackend").Text = T("⚙️ Rafitra ambadika", "⚙️ Backend", "⚙️ Backend");

            N<Button>("BtnBGps").Text = "📍 GPS";
            N<Button>("BtnBInv").Text = T("🎒 Kitapo", "🎒 Inventaire", "🎒 Inventory");
            N<Button>("BtnBJournal").Text = T("📓 Diarin'ny dia", "📓 Journal", "📓 Journal");
            N<Button>("BtnBCarte").Text = T("🗺️ Sarintany", "🗺️ Carte", "🗺️ Map");
            N<Button>("BtnBCodes").Text = T("🔐 Kaody", "🔐 Codes", "🔐 Codes");
            N<Button>("BtnBSos").Text = "🆘 SOS";
            N<Button>("BtnBHome").Text = T("🏠 Fandraisana", "🏠 Accueil", "🏠 Home");

            N<Label>("AuthTitre").Text = T("🔐 Fidirana — tsy misy kaonty voasoratra mialoha", "🔐 Connexion — aucun compte pré-rempli", "🔐 Login — no account pre-filled");
            N<Button>("BtnAuthLogin").Text = T("Fidirana", "Connexion", "Login");
            N<Button>("BtnAuthRegister").Text = T("Mamorona kaonty", "Créer un compte", "Create an account");
            N<Button>("BtnAuthFirst").Text = T("Mpitantana Ambony voalohany", "Premier Super Admin", "First Super Admin");
            _libelleCle.Text = T("Lakile fanafenana — angatahina alohanina", "Clé de chiffrement — obligatoire d'abord", "Encryption key — required first");
            _editCle.PlaceholderText = T("Lakile", "Clé", "Key");
            N<Label>("LblLoginPseudo").Text = T("Anarana fidirana", "Pseudo", "Username");
            N<LineEdit>("EditLoginPseudo").PlaceholderText = T("Anarana", "Pseudo", "Username");
            N<Label>("LblLoginPass").Text = T("Tenimiafina — 8 litera farafahakeliny", "Mot de passe — 8 caractères minimum", "Password — 8 characters minimum");
            N<LineEdit>("EditLoginPass").PlaceholderText = T("Tenimiafina", "Mot de passe", "Password");
            N<Button>("BtnLogin").Text = T("🔐 Miditra", "🔐 Se connecter", "🔐 Log in");
            N<Label>("LblLoginHint").Text = T("Tsy misy kaonty ? Mamorona aloha ny Mpitantana Ambony voalohany.", "Aucun compte ? Créez d'abord le premier Super Admin.", "No account? Create the first Super Admin first.");
            N<Label>("LblRegPseudo").Text = T("Anarana fidirana", "Pseudo", "Username");
            N<LineEdit>("EditRegPseudo").PlaceholderText = T("Anarana", "Pseudo", "Username");
            N<Label>("LblRegPass").Text = T("Tenimiafina", "Mot de passe", "Password");
            N<LineEdit>("EditRegPass").PlaceholderText = T("Tenimiafina", "Mot de passe", "Password");
            N<Label>("LblRegRole").Text = T("Andraikitra angatahina", "Rôle demandé", "Requested role");
            OptionButton roles = N<OptionButton>("OptRegRole");
            roles.SetItemText(0, T("🧑 Mpikambana", "🧑 Membre", "🧑 Member"));
            roles.SetItemText(1, T("🧭 Mpitarika", "🧭 Chef", "🧭 Chief"));
            roles.SetItemText(2, T("🎭 Mpanentana", "🎭 Animateur", "🎭 Animator"));
            N<Label>("LblRegEquipe").Text = T("Ekipa (anarana malalaka)", "Équipe (nom libre)", "Team (free name)");
            N<Label>("LblRegRoleEq").Text = T("Andraikitra ao amin'ny ekipa", "Rôle dans l'équipe", "Role in the team");
            N<Label>("LblRegParrain").Text = T("Iza no nanasa anao ? Anaran'ny mpitarika", "Qui t'a invité ? Nom du chef", "Who invited you? Chief's name");
            N<Label>("LblRegParent").Text = T("Ray aman-dreny — anarana sy contact", "Parent — nom et contact", "Parent — name and contact");
            N<CheckBox>("CheckConsentement").Text = T("✅ Manaiky ny ray aman-dreny — tsy maintsy misy", "✅ Consentement parental — obligatoire", "✅ Parental consent — required");
            N<Button>("BtnRegister").Text = T("➕ Mangataka famoronana", "➕ Demander la création", "➕ Request an account");
            N<Label>("LblFirstPseudo").Text = T("Anaran'ny Mpitantana Ambony voalohany", "Pseudo du premier Super Admin", "First Super Admin username");
            N<Label>("LblFirstPass").Text = T("Tenimiafina", "Mot de passe", "Password");
            N<Button>("BtnFirst").Text = T("👑 Mamorona ny Mpitantana Ambony voalohany", "👑 Créer le premier Super Admin", "👑 Create the first Super Admin");
        }

        private void ChargerPerso()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            Personnalisation p = _gm.GetPerso(u.Pseudo);
            N<LineEdit>("EditPersoNom").Text = p.NomPersonnage;
            SelectionnerTexte(N<OptionButton>("OptPersoAvatar"), p.AvatarEmoji);
            SelectionnerTexte(N<OptionButton>("OptPersoTenue"), p.Tenue);
            SelectionnerTexte(N<OptionButton>("OptPersoFoulard"), p.Foulard);
            SelectionnerTexte(N<OptionButton>("OptPersoArme"), p.Arme);
            SelectionnerTexte(N<OptionButton>("OptPersoGenre"), p.Genre);
            SelectionnerTexte(N<OptionButton>("OptPersoCheveux"), p.StyleCheveux);
            N<SpinBox>("EditPersoTaille").Value = p.Taille;
            N<Label>("PersoApercu").Text = p.AvatarEmoji + " " + p.NomPersonnage;
        }

        private void OnPersoSauver()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            var perso = new Personnalisation
            {
                NomPersonnage = N<LineEdit>("EditPersoNom").Text.Trim(),
                AvatarEmoji = Texte(N<OptionButton>("OptPersoAvatar")),
                Tenue = Texte(N<OptionButton>("OptPersoTenue")),
                Foulard = Texte(N<OptionButton>("OptPersoFoulard")),
                Arme = Texte(N<OptionButton>("OptPersoArme")),
                Genre = Texte(N<OptionButton>("OptPersoGenre")),
                StyleCheveux = Texte(N<OptionButton>("OptPersoCheveux")),
                Taille = (float)N<SpinBox>("EditPersoTaille").Value
            };
            N<Label>("LblPersoMsg").Text = _gm.SauverPersonnalisation(u.Pseudo, perso);
            ChargerPerso();
        }

        private void RafraichirCamp()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            Campagne? campagne = _gm.Data.Campagne;
            Meteo meteo = campagne?.MeteoActuelle ?? Meteo.Chaleur;

            N<Label>("CampTitre").Text = T("🏕️ Tobi", "🏕️ Camp", "🏕️ Camp");
            N<Label>("LblFeu").Text = T("🔥 Afo", "🔥 Feu", "🔥 Fire") + "\n" + _feu + "%";
            N<Label>("LblEau").Text = T("💧 Rano", "💧 Eau", "💧 Water") + "\n—";
            N<Label>("LblBois").Text = T("🪵 Kitay", "🪵 Bois", "🪵 Wood") + "\n" + _bois;
            N<Label>("LblTente").Text = T("⛺ Lay", "⛺ Tente", "⛺ Tent") + "\n—";
            N<Button>("BtnFeu").Text = T("+ Kitay", "+ Bois", "+ Wood");
            N<Button>("BtnNuit").Text = _nuit
                ? T("🌞 Andro", "🌞 Jour", "🌞 Day")
                : T("🌙 Alina", "🌙 Nuit", "🌙 Night");
            N<Button>("BtnGps").Text = T("📍 GPS tena izy", "📍 GPS réel", "📍 Real GPS");
            N<Label>("LblGps").Text = _gps.Actif
                ? T("GPS : aktif", "GPS : actif", "GPS: active") + " ±" + _gps.Precision.ToString("0", CultureInfo.InvariantCulture) + " m"
                : T("GPS : tsy mbola navadika", "GPS : non activé", "GPS: not enabled");

            List<Poi> zones = _gm.ZonesInterdites();
            N<Label>("LblZone").Text = T("⚠️ Faritra voarara", "⚠️ Zone interdite", "⚠️ Restricted zone") + " : "
                + (zones.Count == 0 ? T("Tsy misy", "Aucune", "None") : string.Join(", ", zones.Select(z => z.Nom)));
            N<Button>("BtnSos").Text = T("🆘 SOS — SOS ihany", "🆘 SOS — seul le SOS est envoyé", "🆘 SOS — only the SOS is sent");

            N<Label>("LblClassement").Text = T("📊 Filaharana", "📊 Classement", "📊 Ranking");
            var classement = _gm.Data.Progressions
                .Where(p => p.Score > 0)
                .OrderByDescending(p => p.Score)
                .Select((p, i) => (i + 1) + ". " + p.Cle + " — " + p.Score + " pts");
            string listeClassement = string.Join("\n", classement);
            N<Label>("LblClassementList").Text = listeClassement.Length == 0
                ? T("Tsy misy isa", "Aucun score", "No score")
                : listeClassement;

            List<string> badges = _gm.ProgressionCourante().Badges;
            N<Label>("LblBadges").Text = T("🎖️ Mari-pankasitrahana", "🎖️ Badges", "🎖️ Badges") + " : "
                + (badges.Count == 0 ? T("Tsy misy", "Aucun", "None") : string.Join(", ", badges));

            N<Label>("CampagneTitre").Text = T(
                "📅 Lasy — Sokajy : Tselatra 1–3, Lehibe 1–5, Feno 1–11",
                "📅 Camp — Type : Tselatra 1–3, Lehibe 1–5, Feno 1–11",
                "📅 Camp — Type: Tselatra 1–3, Lehibe 1–5, Feno 1–11");
            N<Label>("LblCampTypeInfo").Text = T(
                "⚡ Tselatra 1–3 andro, 🏕️ Lehibe 1–5 andro, 🌲 Feno 1–11 andro",
                "⚡ Tselatra 1–3 jours, 🏕️ Lehibe 1–5 jours, 🌲 Feno 1–11 jours",
                "⚡ Tselatra 1–3 days, 🏕️ Lehibe 1–5 days, 🌲 Feno 1–11 days");
            N<Label>("LblCampType").Text = T("Sokajin'ny lasy", "Type de camp", "Camp type");
            N<Label>("LblCampJours").Text = T("Isan'ny andro (1–11)", "Nombre de jours (1–11)", "Number of days (1–11)");
            N<Label>("LblCampDebut").Text = T("Daty fanombohana", "Date de début", "Start date");
            N<Button>("BtnCampCreer").Text = T("✅ Mamorona lasy", "✅ Créer la campagne", "✅ Create the camp");
            N<Button>("BtnCampPause").Text = T("⏸️ Atsaharo", "⏸️ Pause", "⏸️ Pause");
            N<Button>("BtnCampReprendre").Text = T("▶️ Tohizo", "▶️ Reprendre", "▶️ Resume");
            N<Button>("BtnCampFin").Text = T("🏁 Vitao", "🏁 Terminer", "🏁 End");
            N<Label>("LblCampagneInfo").Text = campagne == null
                ? T("Tsy misy lasy", "Aucune campagne", "No camp")
                : LibelleType(campagne.Type) + " — " + LibelleStatut(campagne.Statut) + " — "
                    + T("andro", "jour", "day") + " " + campagne.JourActuel + "/" + campagne.Jours + " — " + campagne.Debut;

            bool gere = u.Role == Role.SuperAdmin || u.Role == Role.Chef;
            N<Control>("CampagneForm").Visible = gere;
            N<Control>("CampagneBtns").Visible = gere;
            N<Button>("BtnCampCreer").Disabled = campagne != null && campagne.Statut != StatutCampagne.Finished;
            N<Button>("BtnCampPause").Disabled = campagne?.Statut != StatutCampagne.Running;
            N<Button>("BtnCampReprendre").Disabled = !(campagne?.Statut is StatutCampagne.Scheduled or StatutCampagne.Paused);
            N<Button>("BtnCampFin").Disabled = !(campagne?.Statut is StatutCampagne.Running or StatutCampagne.Paused);

            _lblMeteo.Text = T("🌦️ Toetr'andro", "🌦️ Météo", "🌦️ Weather") + " : " + LibelleMeteo(meteo);
            _lblEnergie.Text = T("⚡ Hery", "⚡ Énergie", "⚡ Energy") + " : " + _gm.EnergieDe(u) + "/100 — "
                + T("🍲 Sakafo", "🍲 Rations", "🍲 Rations") + " : " + u.Rations;
            _btnManger.Text = T("🍲 Nihinana", "🍲 Manger", "🍲 Eat");
            _btnMeteo.Text = T("🌦️ Ovaina ny toetr'andro", "🌦️ Changer la météo", "🌦️ Change weather");
            _btnJourSuivant.Text = T("📅 Andro manaraka", "📅 Jour suivant", "📅 Next day");
            _btnJourSuivant.Visible = u.Role == Role.SuperAdmin;
            _btnJourSuivant.Disabled = campagne?.Statut != StatutCampagne.Running;
            RafraichirVieEquipe();

            Meteo[] valeursMeteo = Enum.GetValues<Meteo>();
            for (int i = 0; i < valeursMeteo.Length; i++) _optMeteo.SetItemText(i, LibelleMeteo(valeursMeteo[i]));

            N<Control>("EndScreen").Visible = campagne?.Statut == StatutCampagne.Finished && !_finMasquee;
            N<Label>("LblEndNom").Text = T("🎉 Tahafinaritra", "🎉 Bravo", "🎉 Well done") + ", " + u.Pseudo + " !";
            N<Label>("LblEndScore").Text = _gm.ScoreCourant() + " pts";
            N<Label>("LblEndTotem").Text = string.Join(", ", _gm.ProgressionCourante().Badges);
        }

        private void OnBois()
        {
            _bois++;
            _feu = Math.Min(100, _feu + 10);
            RafraichirCamp();
        }

        private void OnBasculerNuit()
        {
            _nuit = !_nuit;
            RafraichirCamp();
        }

        private void OnGps()
        {
            SetStatus(_gps.Actif
                ? "📍 GPS aktif"
                : "📍 Tsy misy loharano GPS amin'ity version / Aucune source GPS dans cette version");
            RafraichirCamp();
        }

        private void OnSos()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            bool envoye = _gps.EnvoyerSos(u.Equipe);
            SetStatus(envoye
                ? "🆘 SOS enregistré — chef informé"
                : "❌ Pas de position GPS — SOS non envoyé");
        }

        private void OnCampagneCreer()
        {
            TypeLasy type = N<OptionButton>("OptCampType").Selected switch
            {
                1 => TypeLasy.Lehibe,
                2 => TypeLasy.Feno,
                _ => TypeLasy.Tselatra
            };
            int jours = (int)N<SpinBox>("EditCampJours").Value;
            SetStatus(_gm.CreerCampagne(jours, N<LineEdit>("EditCampDebut").Text.Trim(), type));
            RafraichirTout();
        }

        private void OnCampagneEtat(StatutCampagne cible)
        {
            SetStatus(_gm.ChangerStatutCampagne(cible));
            RafraichirTout();
        }

        private void RafraichirComptes()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;
            bool sa = u.Role == Role.SuperAdmin;

            N<Label>("LblCompteInfo").Text = "👤 " + u.Pseudo + " — " + LibelleRole(u.Role) + " (" + u.Statut + ")";
            N<Control>("SuperPanel").Visible = sa;
            GetNode<Control>(Racine + "ComptesPanel/ComptesVBox/ChefPanel").Visible = u.Role == Role.Chef;

            ViderBoite(_boiteDemandes);
            ViderBoite(_boiteActivitesAttente);
            List<User> attente = _gm.Data.Users.Where(x => x.Statut == StatutCompte.EnAttente).ToList();
            N<Label>("LblAttente").Text = T("⏳ Miandry", "⏳ En attente", "⏳ Pending") + " (" + attente.Count + ")";
            if (sa)
            {
                foreach (User demande in attente)
                {
                    string pseudo = demande.Pseudo;
                    AjouterLigneValidation(_boiteDemandes, pseudo + " (" + demande.Role + ")",
                        () => { SetStatus(_gm.ValiderUser(pseudo, true)); RafraichirTout(); },
                        () => { SetStatus(_gm.ValiderUser(pseudo, false)); RafraichirTout(); });
                }
                foreach (Activite a in _gm.Data.Activites.Where(x => x.Statut == "en_attente_validation"))
                {
                    string id = a.Id;
                    AjouterLigneValidation(_boiteActivitesAttente, a.Titre + " — " + a.TarikaNom,
                        () => { SetStatus(_gm.ValiderDemandeActivite(id, true)); RafraichirTout(); },
                        () => { SetStatus(_gm.ValiderDemandeActivite(id, false)); RafraichirTout(); });
                }
            }
            N<Label>("LblDemandes").Text = attente.Count == 0 ? T("Tsy misy fangatahana", "Aucune demande", "No request") : "";
            N<Label>("LblDemandesActivites").Text = T("⏳ Fangatahana asa", "⏳ Demandes d'activités", "⏳ Activity requests");
            N<Label>("LblDemandesActivitesList").Visible = false;

            List<User> tous = _gm.Data.Users;
            N<Label>("LblUsers").Text = T("👥 Mpampiasa rehetra", "👥 Tous les utilisateurs", "👥 All users") + " (" + tous.Count + ")";
            N<Label>("LblUsersList").Text = string.Join("\n", tous.Select(x => x.Pseudo + " — " + x.Role + " — " + x.Statut));

            N<Label>("LblMesCreations").Text = T("Ny mpikambanako : ", "Mes membres : ", "My members: ")
                + string.Join(", ", tous.Where(x => x.Parrain == u.Pseudo).Select(x => x.Pseudo));
            N<Button>("BtnChefCreer").Disabled = true;
            N<Label>("LblChef").Text = T("🧭 Mpitarika — ny fisoratra no mamorona mpikambana", "🧭 Chef — les membres s'inscrivent eux-mêmes", "🧭 Leader — members register themselves");
            N<Button>("BtnChefCreer").Text = T("🧭 Miandry fanamarinana", "🧭 Créer (en attente)", "🧭 Create (pending)");
        }

        private static void AjouterLigneValidation(VBoxContainer boite, string texte, Action valider, Action refuser)
        {
            var ligne = new HBoxContainer();
            ligne.AddChild(new Label
            {
                Text = texte,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            var boutonValider = new Button { Text = "✅", CustomMinimumSize = new Vector2(56, 44) };
            var boutonRefuser = new Button { Text = "❌", CustomMinimumSize = new Vector2(56, 44) };
            boutonValider.Pressed += valider;
            boutonRefuser.Pressed += refuser;
            ligne.AddChild(boutonValider);
            ligne.AddChild(boutonRefuser);
            boite.AddChild(ligne);
        }

        private static void AjouterLigneSimple(VBoxContainer boite, string texte, string libelleBouton, Action action)
        {
            var ligne = new HBoxContainer();
            ligne.AddChild(new Label
            {
                Text = texte,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            var bouton = new Button { Text = libelleBouton, CustomMinimumSize = new Vector2(120, 44) };
            bouton.Pressed += action;
            ligne.AddChild(bouton);
            boite.AddChild(ligne);
        }

        private void OnCreerCompteSuperAdmin()
        {
            Role role = N<OptionButton>("OptSaRole").Selected switch
            {
                0 => Role.Chef,
                1 => Role.Animateur,
                _ => Role.SuperAdmin
            };
            SetStatus(_gm.CreateUser(
                N<LineEdit>("EditSaPseudo").Text,
                N<LineEdit>("EditSaPass").Text,
                role,
                "",
                "",
                "",
                true,
                false,
                "",
                ""));
            RafraichirTout();
        }

        private void RafraichirTarika()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            N<Label>("LblTarikaList").Text = _gm.Data.Tarikas.Count == 0
                ? T("Tsy misy tarika", "Aucun groupe", "No group")
                : string.Join("\n", _gm.Data.Tarikas.Select(t =>
                    t.Embleme + " " + t.Nom + " — " + T("mpitarika", "chef", "leader") + " " + t.ChefPseudo + " — " + _gm.CompterTarika(t.Id) + " " + T("olona", "personnes", "people")));

            OptionButton chefs = N<OptionButton>("OptTarikaChef");
            chefs.Clear();
            foreach (User c in _gm.Data.Users.Where(x => x.Role == Role.Chef && x.Statut == StatutCompte.Valide && string.IsNullOrEmpty(x.TarikaId)))
            {
                chefs.AddItem(c.Pseudo);
            }
            if (chefs.ItemCount == 0) chefs.AddItem(T("Tsy misy mpitarika tsy manan-tarika", "Aucun chef sans groupe", "No leader without a group"));
        }

        private void OnTarikaCreer()
        {
            SetStatus(_gm.CreerTarika(
                N<LineEdit>("EditTarikaNom").Text,
                Texte(N<OptionButton>("OptTarikaChef")),
                "#2E7D32",
                Texte(N<OptionButton>("OptTarikaEmbleme")),
                N<LineEdit>("EditTarikaDevise").Text));
            RafraichirTout();
        }

        private void RafraichirCarte()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            N<Label>("LblPoiList").Text = _gm.Data.Pois.Count == 0
                ? T("📍 Toerana : tsy misy", "📍 Lieux : aucun", "📍 Places: none")
                : T("📍 Toerana :", "📍 Lieux :", "📍 Places:") + "\n" + string.Join("\n", _gm.Data.Pois.Select(p =>
                    (p.Interdit ? "🔴 " : "🟢 ") + p.Nom + " (" + p.Lat.ToString("0.0000", CultureInfo.InvariantCulture)
                    + ", " + p.Lon.ToString("0.0000", CultureInfo.InvariantCulture) + ")"));

            _nomsLieux.Clear();
            OptionButton lieux = N<OptionButton>("OptNLieu");
            lieux.Clear();
            foreach (Poi p in _gm.Data.Pois)
            {
                _nomsLieux.Add(p.Nom);
                lieux.AddItem(p.Nom);
            }
            if (lieux.ItemCount == 0) lieux.AddItem(T("Tsy misy toerana", "Aucun lieu — crées-en d'abord", "No place — create one first"));
        }

        private void OnPoiCreer()
        {
            bool latOk = double.TryParse(N<LineEdit>("EditPoiLat").Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat);
            bool lonOk = double.TryParse(N<LineEdit>("EditPoiLon").Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon);
            if (!latOk || !lonOk)
            {
                SetStatus("❌ Latitude / Longitude diso / incorrect");
                return;
            }
            SetStatus(_gm.CreerPoi(
                N<LineEdit>("EditPoiNom").Text,
                lat,
                lon,
                N<OptionButton>("OptPoiInterdit").Selected == 1,
                N<LineEdit>("EditPoiDesc").Text));
            RafraichirTout();
        }

        private void RafraichirActivites()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            List<Activite> visibles = _gm.GetActivitesVisibles(u);
            N<Label>("LblActivitesList").Text = visibles.Count == 0
                ? T("Tsy misy asa — ny mpanentana no mamorona", "Aucune activité — l'animateur en crée", "No activity — the organiser creates them")
                : string.Join("\n", visibles.Select(a =>
                    "[" + a.Statut + "] J" + a.Jour + " " + a.Titre + " — " + a.Points + " pts — " + a.TarikaNom));

            _activiteAffichee = _gm.ActiviteActuelle();
            if (_activiteAffichee == null)
            {
                N<Label>("LblEnigme").Text = T("⏳ Tsy misy asa azo atao amin'izao", "⏳ Aucune activité jouable maintenant (camp, jour ou horaire)", "⏳ No activity can be played right now (camp, day or time)");
                N<Label>("LblEssais").Text = "";
                return;
            }

            Activite a = _activiteAffichee;
            N<Label>("LblEnigme").Text = "🎯 " + a.Titre + " — Jour " + a.Jour + "\n🔐 " + a.TexteCode;
            int essais = _gm.ProgressionCourante().Essais.GetValueOrDefault(a.Id);
            N<Label>("LblEssais").Text = T("Andrana", "Essais", "Attempts") + " : " + essais + " / " + a.EssaisMax;
        }

        private void OnRepondre()
        {
            if (_activiteAffichee == null)
            {
                N<Label>("LblReponseMsg").Text = T("❌ Tsy misy asa", "❌ Aucune activité", "❌ No activity");
                return;
            }
            string reponse = N<LineEdit>("EditReponse").Text;
            if (string.IsNullOrWhiteSpace(reponse))
            {
                N<Label>("LblReponseMsg").Text = T("❌ Valiny banga", "❌ Réponse vide", "❌ Empty answer");
                return;
            }
            _gm.Repondre(_activiteAffichee, reponse, out string retour, out _);
            N<Label>("LblReponseMsg").Text = retour;
            N<LineEdit>("EditReponse").Text = "";
            RafraichirTout();
        }

        private void OnEncoder()
        {
            string code = Texte(N<OptionButton>("OptCodeChoix"));
            try
            {
                string resultat = CodeEncoder.Encode(code, N<LineEdit>("EditMsgClair").Text, N<LineEdit>("EditParam").Text);
                N<Label>("LblCodeResult").Text = resultat.Length == 0 ? "…" : resultat;
                N<Label>("LblCodeAide").Text = T("Kaody", "Code", "Code") + " : " + code;
            }
            catch (NotSupportedException ex)
            {
                N<Label>("LblCodeResult").Text = "❌ " + ex.Message;
                N<Label>("LblCodeAide").Text = "";
            }
        }

        private void RafraichirTeknika()
        {
            RafraichirOptionsTeknika();
            string filtre = N<LineEdit>("EditTekSearch").Text.Trim();
            var lignes = _teknikas
                .Where(t => filtre.Length == 0
                    || t.Titre.Contains(filtre, StringComparison.OrdinalIgnoreCase)
                    || t.Categorie.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                .Select(t => Pick(t.Titre) + " — " + Pick(t.Categorie) + " (" + t.Page + ")");
            string texte = string.Join("\n", lignes);
            N<Label>("LblTekList").Text = texte.Length == 0 ? T("Tsy hita", "Aucun résultat", "No result") : texte;
        }

        private void ConstruireConsultation()
        {
            Node bouton = N<Node>("BtnQuiz");
            Node parent = bouton.GetParent();
            N<HBoxContainer>("QuizOpts").Visible = false;
            N<Label>("LblQuizMsg").Text = "";
            _optConsult = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
            foreach (Teknika t in _teknikas) _optConsult.AddItem(t.Categorie + " — " + t.Titre + " — " + t.Page);
            if (_optConsult.ItemCount > 0) _optConsult.Select(0);
            _btnUtiliser = new Button { Text = "➡️ Ampiasao amin'ny asa / Utiliser pour une activité", CustomMinimumSize = new Vector2(0, 44) };
            _btnUtiliser.Pressed += OnUtiliserCode;
            parent.AddChild(_optConsult);
            parent.MoveChild(_optConsult, bouton.GetIndex());
            parent.AddChild(_btnUtiliser);
            parent.MoveChild(_btnUtiliser, bouton.GetIndex() + 1);
            N<Button>("BtnQuiz").Text = "🔍 Hitrandraka / Consulter";
        }

        private void OnConsulter()
        {
            if (_optConsult.ItemCount == 0 || _optConsult.Selected < 0) return;
            Teknika t = _teknikas[_optConsult.Selected];
            bool utilisable = CodeEncoder.CodesSupportes.Contains(t.Code);
            N<Label>("LblQuizQ").Text = T("🧭 Toerana : Teknika Skoto", "🧭 Chemin : Teknika Skoto", "🧭 Path: Teknika Skoto")
                + " > " + Pick(t.Categorie) + " > " + t.Page
                + "\n📖 " + Pick(t.Titre)
                + "\n" + SansBarre(T(t.DescMg, t.DescFr, t.DescEn))
                + "\n" + SansBarre(T(t.UsageMg, t.UsageFr, t.UsageEn))
                + "\n🔑 " + T("Kaody", "Code", "Code") + " : " + (string.IsNullOrEmpty(t.Code) ? "—" : t.Code)
                + (utilisable
                    ? T(" — azo ampiasaina amin'ny Mamorona", " — utilisable dans Créer", " — usable in Create")
                    : T(" — consigne ihany", " — consigne seulement", " — instruction only"));
            N<Label>("LblQuizMsg").Text = "";
        }

        private void OnUtiliserCode()
        {
            if (_optConsult.ItemCount == 0 || _optConsult.Selected < 0) return;
            Teknika t = _teknikas[_optConsult.Selected];
            Label message = N<Label>("LblQuizMsg");
            if (!CodeEncoder.CodesSupportes.Contains(t.Code))
            {
                message.Text = "❌ Tsy kaody izy / Ce n'est pas un code : utilisez-le comme consigne / consigne seulement";
                return;
            }
            int index = -1;
            for (int i = 0; i < N<OptionButton>("OptNCode").ItemCount; i++)
            {
                if (N<OptionButton>("OptNCode").GetItemText(i) == t.Code) index = i;
            }
            if (index < 0)
            {
                message.Text = "❌ Kaody tsy hita ao amin'ny Mamorona / Code absent de Créer";
                return;
            }
            if (_gm.CurrentUser == null || (_gm.CurrentUser.Role != Role.SuperAdmin && _gm.CurrentUser.Role != Role.Chef && _gm.CurrentUser.Role != Role.Animateur))
            {
                message.Text = "❌ Tsy manana fahazoan-dalana / Vous n'avez pas le droit de créer une activité";
                return;
            }
            N<OptionButton>("OptNCode").Select(index);
            ChoisirPanneau("Creer");
            message.Text = "✅ Voafidy ho an'ny asa / Code sélectionné pour l'activité : " + t.Code
                + (t.Code == "Semaphore" ? " — cocher « Activité navale »" : "");
        }

        private void RafraichirEquipes()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            var lignes = new List<string>();
            foreach (User chef in _gm.Data.Users.Where(x => x.Role == Role.Chef))
            {
                Tarika? tarika = _gm.GetTarikaDe(chef.Pseudo);
                lignes.Add("🧭 " + chef.Pseudo + " — " + (tarika?.Nom ?? "tsy misy tarika / pas de groupe"));
                var membres = _gm.Data.Users.Where(x => x.Role == Role.Membre && x.Parrain == chef.Pseudo);
                foreach (IGrouping<string, User> equipe in membres.GroupBy(x => x.Equipe))
                {
                    lignes.Add("   👥 " + equipe.Key + " : " + string.Join(", ", equipe.Select(m => m.Pseudo)));
                }
            }
            N<Label>("LblEquipesList").Text = lignes.Count == 0 ? T("Tsy misy ekipa", "Aucune équipe", "No team") : string.Join("\n", lignes);
        }

        private void RafraichirChef()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            N<Label>("LblPositions").Text = _gm.Data.DernierSos is SosEnregistre s
                ? "🆘 " + s.Equipe + " — " + s.Date.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)
                    + " — (" + s.Lat.ToString("0.0000", CultureInfo.InvariantCulture) + ", " + s.Lon.ToString("0.0000", CultureInfo.InvariantCulture) + ")"
                : T("Tsy misy SOS", "Aucun SOS", "No SOS");

            List<AuditLog> alertes = _gm.GetAudit().Where(l => l.Action == "ALERTE_ZONE").TakeLast(5).ToList();
            N<Label>("LblAlertes").Text = alertes.Count == 0
                ? T("Tsy misy", "Aucune", "None")
                : string.Join("\n", alertes.Select(l => l.Date.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture) + " — " + l.Detail));
        }

        private void OnTestAlerte()
        {
            _gm.SignalerAlerte("Fitsapana / Test", 0);
            RafraichirTout();
        }

        private void RafraichirAudit()
        {
            List<AuditLog> journal = _gm.GetAudit().TakeLast(20).Reverse().ToList();
            N<Label>("LblAudit").Text = journal.Count == 0
                ? T("Tsy misy audit", "Aucun audit", "No audit")
                : string.Join("\n", journal.Select(l =>
                    l.Date.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture) + " — " + l.Pseudo + " — " + l.Action + " — " + l.Detail));
        }

        private void RafraichirCreer()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            string droit = u.Role switch
            {
                Role.SuperAdmin => T("👑 Mamorona sy manamarina ny asa rehetra", "👑 Tout créer et valider", "👑 Create and validate all activities"),
                Role.Chef => T("🧭 Asa tarika avy hatrany ; asa rehetra sy cross → fanamarinana SA", "🧭 Activités de groupe immédiates ; rehetra et cross → validation SA", "🧭 Group activities immediate; all and cross → Super Admin validation"),
                Role.Animateur => T("🎭 Mamorona brouillon, avy eo mampiditra", "🎭 Crée des brouillons puis publie", "🎭 Create drafts, then publish"),
                _ => T("🚫 Tsy manana fahazoan-dalana", "🚫 Pas de droit de création", "🚫 No permission to create")
            };
            N<Label>("LblCreerDroit").Text = droit;
            N<Label>("LblCreerQuota").Text = u.Role == Role.Chef
                ? T("Quota asa rehetra", "Quota activités tous", "All-groups quota") + " : " + _gm.CompterAsaRehetraCetteSemaine(u.Pseudo) + " / "
                    + GameManager.QUOTA_ASA_REHETRA_PAR_SEMAINE + T(" tamin'ity herinandro ity", " cette semaine", " this week")
                : "";

            ViderBoite(_boiteBrouillons);
            foreach (Activite a in _gm.Data.Activites.Where(x => x.Statut == "brouillon" && (u.Role == Role.SuperAdmin || x.CreePar == u.Pseudo)))
            {
                string id = a.Id;
                AjouterLigneSimple(_boiteBrouillons, a.Titre + " — J" + a.Jour, T("📢 Navoaka", "📢 Publier", "📢 Publish"), () =>
                {
                    SetStatus(_gm.PublierActivite(id));
                    RafraichirTout();
                });
            }
            N<Label>("LblBrouillons").Text = T("📝 Ny volavolako", "📝 Mes brouillons", "📝 My drafts");

            int attente = _gm.Data.Activites.Count(x => x.Statut == "en_attente_validation" && x.CreePar == u.Pseudo);
            N<Label>("LblDemandesValidation").Text = T("⏳ Fangatahana miandry", "⏳ Demandes en attente", "⏳ Pending requests") + " : " + attente;

            OptionButton tarikaCible = N<OptionButton>("OptNTarikaCible");
            tarikaCible.Clear();
            _idsTarikasCibles.Clear();
            foreach (Tarika t in _gm.Data.Tarikas)
            {
                _idsTarikasCibles.Add(t.Id);
                tarikaCible.AddItem(t.Embleme + " " + t.Nom + " — " + t.ChefPseudo);
            }
            if (tarikaCible.ItemCount == 0) tarikaCible.AddItem(T("Tsy misy tarika", "Aucun groupe", "No group"));

            RafraichirApercu();
        }

        private static string ParametreParDefaut(string code) => code switch
        {
            "Cesar" => "3",
            "Vigenere" => "CLE",
            _ => ""
        };

        private void RafraichirApercu()
        {
            string code = Texte(N<OptionButton>("OptNCode"));
            try
            {
                string apercu = CodeEncoder.Encode(code, N<LineEdit>("EditNClair").Text, ParametreParDefaut(code));
                N<Label>("LblApercu").Text = "Aperçu joueur : " + (apercu.Length == 0 ? "…" : apercu);
            }
            catch (NotSupportedException ex)
            {
                N<Label>("LblApercu").Text = "❌ " + ex.Message;
            }
        }

        private void OnCreerActivite()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            string[] heure = N<LineEdit>("EditNHeure").Text.Split('-').Select(s => s.Trim()).ToArray();
            string code = Texte(N<OptionButton>("OptNCode"));
            int indexLieu = N<OptionButton>("OptNLieu").Selected;
            int indexTarika = N<OptionButton>("OptNTarikaCible").Selected;

            var activite = new Activite
            {
                Titre = N<LineEdit>("EditNTitre").Text.Trim(),
                Modele = Texte(N<OptionButton>("OptNModele")),
                Code = code,
                Param = ParametreParDefaut(code),
                Clair = N<LineEdit>("EditNClair").Text,
                Points = (int)N<SpinBox>("EditNPoints").Value,
                Jour = (int)N<SpinBox>("EditNJour").Value,
                HeureDebut = heure.Length > 0 ? heure[0] : "",
                HeureFin = heure.Length > 1 ? heure[1] : "",
                Lieu = indexLieu >= 0 && indexLieu < _nomsLieux.Count ? _nomsLieux[indexLieu] : "",
                Teknika = N<LineEdit>("EditNTeknika").Text.Trim(),
                ActiviteNavale = _chkNavale.ButtonPressed,
                EssaisMax = (int)N<SpinBox>("EditNEssais").Value,
                Variante = (Variante)Math.Max(0, N<OptionButton>("OptNVariante").Selected),
                Indices = new List<string>
                {
                    N<LineEdit>("EditNIndice1").Text,
                    N<LineEdit>("EditNIndice2").Text,
                    N<LineEdit>("EditNIndice3").Text
                }.Select(s => s.Trim()).Where(s => s.Length > 0).ToList(),
                Reponses = N<LineEdit>("EditNReponses").Text
                    .Split(';')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList()
            };

            PorteeActivite portee = (PorteeActivite)Math.Max(0, N<OptionButton>("OptNPortee").Selected);
            string tarikaId = indexTarika >= 0 && indexTarika < _idsTarikasCibles.Count ? _idsTarikasCibles[indexTarika] : "";
            bool evenement = N<CheckBox>("CheckNEvenementCommun").ButtonPressed;

            SetStatus(_gm.CreerActivite(activite, tarikaId, portee, evenement));
            RafraichirTout();
        }

        private void OnSauverBackend()
        {
            _backendUrl = N<LineEdit>("EditBackendUrl").Text.Trim();
            _backendCampagne = N<LineEdit>("EditCampagneId").Text.Trim();
            _backendEquipe = N<LineEdit>("EditEquipeId").Text.Trim();
            _backendPseudo = N<LineEdit>("EditPseudoBackend").Text.Trim();
            N<Label>("LblBackendTest").Text = T("💾 Voatahiry ao an-tsaina", "💾 Sauvegardé en mémoire", "💾 Saved in memory");
        }

        private void OnTesterBackend()
        {
            string url = N<LineEdit>("EditBackendUrl").Text.Trim();
            bool valide = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && (uri!.Scheme == "http" || uri.Scheme == "https");
            N<Label>("LblBackendTest").Text = valide
                ? T("✅ Adiresy mety — backend tsy mbola voafatotra", "✅ URL valide — backend non connecté dans cette version", "✅ Valid URL — backend not connected in this version")
                : T("❌ Adiresy diso", "❌ URL invalide", "❌ Invalid URL");
        }

        private void RafraichirBackend()
        {
            N<LineEdit>("EditBackendUrl").Text = _backendUrl;
            N<LineEdit>("EditCampagneId").Text = _backendCampagne;
            N<LineEdit>("EditEquipeId").Text = _backendEquipe;
            N<LineEdit>("EditPseudoBackend").Text = _backendPseudo;
        }
    }
}
