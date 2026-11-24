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
            ["Backend"] = "BackendPanel"
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
            ["Backend"] = "BtnTabBackend"
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
        private Button _btnJourSuivant = null!;
        private LineEdit _editCle = null!;
        private VBoxContainer _boiteDemandes = null!;
        private VBoxContainer _boiteActivitesAttente = null!;
        private VBoxContainer _boiteBrouillons = null!;
        private readonly Dictionary<string, Node> _cache = new();
        private readonly List<string> _nomsLieux = new();
        private readonly List<string> _idsTarikasCibles = new();
        private readonly List<Teknika> _teknikas = TeknikaData.All();
        private readonly Random _rng = new();
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

        private void SetStatus(string texte) => Statut().Text = texte;

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
            var boutonManger = new Button { Text = "🍲 Nihinana / Manger", CustomMinimumSize = new Vector2(0, 44) };
            boutonManger.Pressed += OnManger;
            _optMeteo = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
            foreach (Meteo meteo in Enum.GetValues<Meteo>()) _optMeteo.AddItem(LibelleMeteo(meteo));
            _optMeteo.Select(0);
            var boutonMeteo = new Button { Text = "🌦️ Ovaina ny toetr'andro / Changer la météo", CustomMinimumSize = new Vector2(0, 44) };
            boutonMeteo.Pressed += OnChangerMeteo;
            _btnJourSuivant = new Button { Text = "📅 Andro manaraka / Jour suivant", CustomMinimumSize = new Vector2(0, 44) };
            _btnJourSuivant.Pressed += OnJourSuivant;
            campVBox.AddChild(_lblMeteo);
            campVBox.AddChild(_lblEnergie);
            campVBox.AddChild(boutonManger);
            campVBox.AddChild(_optMeteo);
            campVBox.AddChild(boutonMeteo);
            campVBox.AddChild(_btnJourSuivant);
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

        private static string LibelleMeteo(Meteo meteo) => meteo switch
        {
            Meteo.Chaleur => "☀️ Chaleur",
            Meteo.Pluvieux => "🌧️ Pluvieux",
            Meteo.Venteux => "💨 Venteux",
            Meteo.ChaudHumide => "🥵 Chaud et humide",
            Meteo.VenteuxHumide => "🌬️ Venteux et humide",
            Meteo.ChaudSec => "🔥 Chaud et sec",
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
            GetNode<Control>(Racine + "Panel3D").Visible = false;
            GetNode<Control>(BarreOnglets + "BtnTab3D").Visible = false;
            GetNode<Control>(Racine + "DialogPanel").Visible = false;
            N<Button>("BtnInteract").Visible = false;
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
            N<Button>("BtnQuiz").Pressed += OnNouvelleQuestion;
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
        }

        private void RafraichirEntete()
        {
            Statut().Text = _gm.ErreurStockage ?? "Vonona / Prêt";
            N<Label>("StatusMini").Text = _gm.CurrentUser == null
                ? "Vonona / Prêt"
                : _gm.CurrentUser.Pseudo + " — " + _gm.CurrentUser.Role;
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
            _lblMeteo.Text = "🌦️ Toetr'andro / Météo : " + LibelleMeteo(campagne?.MeteoActuelle ?? Meteo.Chaleur);
            _lblEnergie.Text = "⚡ Hery / Énergie : " + _gm.EnergieDe(u) + "/100 — 🍲 Sakafo / Rations : " + u.Rations;
            _btnJourSuivant.Visible = u.Role == Role.SuperAdmin;
            _btnJourSuivant.Disabled = campagne?.Statut != StatutCampagne.Running;

            N<Label>("LblFeu").Text = "🔥 Afo / Feu\n" + _feu + "%";
            N<Label>("LblEau").Text = "💧 Rano / Eau\n—";
            N<Label>("LblBois").Text = "🪵 Kitay / Bois\n" + _bois;
            N<Label>("LblTente").Text = "⛺ Lay / Tente\n—";
            N<Button>("BtnNuit").Text = _nuit ? "🌞 Andro / Jour" : "🌙 Alina / Nuit";

            N<Label>("LblGps").Text = _gps.Actif
                ? "GPS : aktif ±" + _gps.Precision.ToString("0", CultureInfo.InvariantCulture) + " m"
                : "GPS : tsy mbola navadika / non activé";

            List<Poi> zones = _gm.ZonesInterdites();
            N<Label>("LblZone").Text = "⚠️ Faritra voarara / Zone interdite : "
                + (zones.Count == 0 ? "Tsy misy / Aucune" : string.Join(", ", zones.Select(z => z.Nom)));

            var classement = _gm.Data.Progressions
                .Where(p => p.Score > 0)
                .OrderByDescending(p => p.Score)
                .Select((p, i) => (i + 1) + ". " + p.Cle + " — " + p.Score + " pts");
            string listeClassement = string.Join("\n", classement);
            N<Label>("LblClassementList").Text = listeClassement.Length == 0 ? "Tsy misy isa / Aucun score" : listeClassement;

            List<string> badges = _gm.ProgressionCourante().Badges;
            N<Label>("LblBadges").Text = "🎖️ Mari-pankasitrahana / Badges : " + (badges.Count == 0 ? "Tsy misy / Aucun" : string.Join(", ", badges));

            N<Label>("LblCampagneInfo").Text = campagne == null
                ? "Tsy misy lasy / Aucune campagne"
                : campagne.NomType + " — " + campagne.Statut + " — andro " + campagne.JourActuel + "/" + campagne.Jours + " — " + campagne.Debut;

            bool gere = u.Role == Role.SuperAdmin || u.Role == Role.Chef;
            N<Control>("CampagneForm").Visible = gere;
            N<Control>("CampagneBtns").Visible = gere;
            N<Button>("BtnCampCreer").Disabled = campagne != null && campagne.Statut != StatutCampagne.Finished;
            N<Button>("BtnCampPause").Disabled = campagne?.Statut != StatutCampagne.Running;
            N<Button>("BtnCampReprendre").Disabled = !(campagne?.Statut is StatutCampagne.Scheduled or StatutCampagne.Paused);
            N<Button>("BtnCampFin").Disabled = !(campagne?.Statut is StatutCampagne.Running or StatutCampagne.Paused);

            N<Control>("EndScreen").Visible = campagne?.Statut == StatutCampagne.Finished && !_finMasquee;
            N<Label>("LblEndNom").Text = "🎉 Bravo, " + u.Pseudo + " !";
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

            N<Label>("LblCompteInfo").Text = "👤 " + u.Pseudo + " — " + u.Role + " (" + u.Statut + ")";
            N<Control>("SuperPanel").Visible = sa;
            GetNode<Control>(Racine + "ComptesPanel/ComptesVBox/ChefPanel").Visible = u.Role == Role.Chef;

            ViderBoite(_boiteDemandes);
            ViderBoite(_boiteActivitesAttente);
            List<User> attente = _gm.Data.Users.Where(x => x.Statut == StatutCompte.EnAttente).ToList();
            N<Label>("LblAttente").Text = "⏳ Miandry / En attente (" + attente.Count + ")";
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
            N<Label>("LblDemandes").Text = attente.Count == 0 ? "Tsy misy fangatahana / Aucune demande" : "";
            N<Label>("LblDemandesActivites").Text = "⏳ Fangatahana Asa / Demandes d'activités";
            N<Label>("LblDemandesActivitesList").Visible = false;

            List<User> tous = _gm.Data.Users;
            N<Label>("LblUsers").Text = "👥 Mpampiasa rehetra / Tous les utilisateurs (" + tous.Count + ")";
            N<Label>("LblUsersList").Text = string.Join("\n", tous.Select(x => x.Pseudo + " — " + x.Role + " — " + x.Statut));

            N<Label>("LblMesCreations").Text = "Ny mpikambanako / Mes membres : "
                + string.Join(", ", tous.Where(x => x.Parrain == u.Pseudo).Select(x => x.Pseudo));
            N<Button>("BtnChefCreer").Disabled = true;
            N<Label>("LblChef").Text = "🧭 Mpitarika — Ny fisoratra no mamorona mpikambana / Chef — les membres s'inscrivent eux-mêmes";
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
                ? "Tsy misy tarika / Aucun groupe"
                : string.Join("\n", _gm.Data.Tarikas.Select(t =>
                    t.Embleme + " " + t.Nom + " — chef " + t.ChefPseudo + " — " + _gm.CompterTarika(t.Id) + " olona / personnes"));

            OptionButton chefs = N<OptionButton>("OptTarikaChef");
            chefs.Clear();
            foreach (User c in _gm.Data.Users.Where(x => x.Role == Role.Chef && x.Statut == StatutCompte.Valide && string.IsNullOrEmpty(x.TarikaId)))
            {
                chefs.AddItem(c.Pseudo);
            }
            if (chefs.ItemCount == 0) chefs.AddItem("Tsy misy mpitarika tsy manan-tarika / Aucun chef sans groupe");
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
                ? "📍 Toerana / Lieux : Tsy misy / Aucun"
                : "📍 Toerana / Lieux :\n" + string.Join("\n", _gm.Data.Pois.Select(p =>
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
            if (lieux.ItemCount == 0) lieux.AddItem("Tsy misy toerana / Aucun lieu — crée-en d'abord");
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
                ? "Tsy misy asa / Aucune activité — Ny mpanentana no mamorona / L'animateur en crée"
                : string.Join("\n", visibles.Select(a =>
                    "[" + a.Statut + "] J" + a.Jour + " " + a.Titre + " — " + a.Points + " pts — " + a.TarikaNom));

            _activiteAffichee = _gm.ActiviteActuelle();
            if (_activiteAffichee == null)
            {
                N<Label>("LblEnigme").Text = "⏳ Tsy misy asa azo atao amin'izao / Aucune activité jouable maintenant (lasy, andro ou ora)";
                N<Label>("LblEssais").Text = "";
                return;
            }

            Activite a = _activiteAffichee;
            N<Label>("LblEnigme").Text = "🎯 " + a.Titre + " — Jour " + a.Jour + "\n🔐 " + a.TexteCode;
            int essais = _gm.ProgressionCourante().Essais.GetValueOrDefault(a.Id);
            N<Label>("LblEssais").Text = "Andrana / Essais : " + essais + " / " + a.EssaisMax;
        }

        private void OnRepondre()
        {
            if (_activiteAffichee == null)
            {
                N<Label>("LblReponseMsg").Text = "❌ Tsy misy asa / Aucune activité";
                return;
            }
            string reponse = N<LineEdit>("EditReponse").Text;
            if (string.IsNullOrWhiteSpace(reponse))
            {
                N<Label>("LblReponseMsg").Text = "❌ Valiny banga / Réponse vide";
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
                N<Label>("LblCodeAide").Text = "Kaody / Code : " + code;
            }
            catch (NotSupportedException ex)
            {
                N<Label>("LblCodeResult").Text = "❌ " + ex.Message;
                N<Label>("LblCodeAide").Text = "";
            }
        }

        private void RafraichirTeknika()
        {
            string filtre = N<LineEdit>("EditTekSearch").Text.Trim();
            var lignes = _teknikas
                .Where(t => filtre.Length == 0
                    || t.Titre.Contains(filtre, StringComparison.OrdinalIgnoreCase)
                    || t.Categorie.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Titre + " — " + t.Categorie + " (" + t.Page + ")");
            string texte = string.Join("\n", lignes);
            N<Label>("LblTekList").Text = texte.Length == 0 ? "Tsy hita / Aucun résultat" : texte;
        }

        private void OnNouvelleQuestion()
        {
            if (_teknikas.Count < 4) return;
            HBoxContainer options = N<HBoxContainer>("QuizOpts");
            ViderBoite(options);

            Teknika bonne = _teknikas[_rng.Next(_teknikas.Count)];
            List<Teknika> leurres = _teknikas.Where(t => t != bonne).OrderBy(_ => _rng.Next()).Take(3).ToList();
            List<Teknika> choix = leurres.Append(bonne).OrderBy(_ => _rng.Next()).ToList();

            N<Label>("LblQuizQ").Text = "❓ Ino ny teknika ? / Quelle technique ?\n" + bonne.UsageFr;
            N<Label>("LblQuizMsg").Text = "";
            foreach (Teknika option in choix)
            {
                bool juste = option == bonne;
                var bouton = new Button
                {
                    Text = option.Titre,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    CustomMinimumSize = new Vector2(0, 48)
                };
                bouton.Pressed += () => N<Label>("LblQuizMsg").Text = juste
                    ? "✅ Marina / Bravo !"
                    : "❌ Diso / Faux — " + bonne.Titre;
                options.AddChild(bouton);
            }
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
            N<Label>("LblEquipesList").Text = lignes.Count == 0 ? "Tsy misy ekipa / Aucune équipe" : string.Join("\n", lignes);
        }

        private void RafraichirChef()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            N<Label>("LblPositions").Text = _gm.Data.DernierSos is SosEnregistre s
                ? "🆘 " + s.Equipe + " — " + s.Date.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)
                    + " — (" + s.Lat.ToString("0.0000", CultureInfo.InvariantCulture) + ", " + s.Lon.ToString("0.0000", CultureInfo.InvariantCulture) + ")"
                : "Tsy misy SOS / Aucun SOS";

            List<AuditLog> alertes = _gm.GetAudit().Where(l => l.Action == "ALERTE_ZONE").TakeLast(5).ToList();
            N<Label>("LblAlertes").Text = alertes.Count == 0
                ? "Tsy misy / Aucune"
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
                ? "Tsy misy audit / Aucun audit"
                : string.Join("\n", journal.Select(l =>
                    l.Date.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture) + " — " + l.Pseudo + " — " + l.Action + " — " + l.Detail));
        }

        private void RafraichirCreer()
        {
            User? u = _gm.CurrentUser;
            if (u == null) return;

            string droit = u.Role switch
            {
                Role.SuperAdmin => "👑 Mamorona sy manamarina ny asa rehetra / Tout créer et valider",
                Role.Chef => "🧭 Asa tarika avy hatrany ; Asa rehetra sy cross → fanamarinana SA",
                Role.Animateur => "🎭 Mamorona brouillon, mampiditra avy eo / Crée des brouillons puis publie",
                _ => "🚫 Tsy manana fahazoan-dalana / Pas de droit de création"
            };
            N<Label>("LblCreerDroit").Text = droit;
            N<Label>("LblCreerQuota").Text = u.Role == Role.Chef
                ? "Quota Asa Rehetra : " + _gm.CompterAsaRehetraCetteSemaine(u.Pseudo) + " / "
                    + GameManager.QUOTA_ASA_REHETRA_PAR_SEMAINE + " tamin'ity herinandro ity / cette semaine"
                : "";

            ViderBoite(_boiteBrouillons);
            foreach (Activite a in _gm.Data.Activites.Where(x => x.Statut == "brouillon" && (u.Role == Role.SuperAdmin || x.CreePar == u.Pseudo)))
            {
                string id = a.Id;
                AjouterLigneSimple(_boiteBrouillons, a.Titre + " — J" + a.Jour, "📢 Navoaka / Publier", () =>
                {
                    SetStatus(_gm.PublierActivite(id));
                    RafraichirTout();
                });
            }
            N<Label>("LblBrouillons").Text = "📝 Ny volavolako / Mes brouillons";

            int attente = _gm.Data.Activites.Count(x => x.Statut == "en_attente_validation" && x.CreePar == u.Pseudo);
            N<Label>("LblDemandesValidation").Text = "⏳ Fangatahana miandry / Demandes en attente : " + attente;

            OptionButton tarikaCible = N<OptionButton>("OptNTarikaCible");
            tarikaCible.Clear();
            _idsTarikasCibles.Clear();
            foreach (Tarika t in _gm.Data.Tarikas)
            {
                _idsTarikasCibles.Add(t.Id);
                tarikaCible.AddItem(t.Embleme + " " + t.Nom + " — " + t.ChefPseudo);
            }
            if (tarikaCible.ItemCount == 0) tarikaCible.AddItem("Tsy misy tarika / Aucun groupe");

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
            N<Label>("LblBackendTest").Text = "💾 Tehirizo ao an-tsaina / Sauvegardé en mémoire";
        }

        private void OnTesterBackend()
        {
            string url = N<LineEdit>("EditBackendUrl").Text.Trim();
            bool valide = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && (uri!.Scheme == "http" || uri.Scheme == "https");
            N<Label>("LblBackendTest").Text = valide
                ? "✅ Adiresy mety / URL valide — backend tsy mbola voafatotra / backend non connecté dans cette version"
                : "❌ URL diso / URL invalide";
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
