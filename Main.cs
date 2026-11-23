using Godot;
using System;
using System.Collections.Generic;

namespace Sahavola
{
    public partial class Main : Control
    {
        private GameManager _gm = null!;
        private ResponsiveManager _resp = null!;
        private GPS _gps = null!;
        private Label _status = null!;
        private Label _statusMini = null!;
        private Label _titre = null!;
        private GridContainer _loginForm = null!;
        private GridContainer _registerForm = null!;
        private GridContainer _firstForm = null!;
        private LineEdit _editLoginPseudo = null!;
        private LineEdit _editLoginPass = null!;
        private LineEdit _editSecret = null!;
        private LineEdit _editRegPseudo = null!;
        private LineEdit _editRegPass = null!;
        private OptionButton _optRegRole = null!;
        private LineEdit _editRegEquipe = null!;
        private LineEdit _editRegRoleEq = null!;
        private LineEdit _editRegParrain = null!;
        private LineEdit _editRegParent = null!;
        private LineEdit _editRegContactParent = null!;
        private CheckBox _checkConsentement = null!;
        private LineEdit _editFirstPseudo = null!;
        private LineEdit _editFirstPass = null!;
        private Label _lblAuthMsg = null!;
        private Label _lblPersoApercu = null!;
        private LineEdit _editPersoNom = null!;
        private OptionButton _optPersoAvatar = null!;
        private OptionButton _optPersoTenue = null!;
        private OptionButton _optPersoFoulard = null!;
        private OptionButton _optPersoArme = null!;
        private SpinBox _editPersoTaille = null!;
        private OptionButton _optPersoGenre = null!;
        private OptionButton _optPersoCheveux = null!;
        private Label _lblPersoMsg = null!;
        private Label _lblTarikaList = null!;
        private LineEdit _editTarikaNom = null!;
        private OptionButton _optTarikaChef = null!;
        private OptionButton _optTarikaEmbleme = null!;
        private LineEdit _editTarikaDevise = null!;
        private Label _lblTarikaMsg = null!;
        private Label _lblFeu = null!;
        private Label _lblEau = null!;
        private Label _lblBois = null!;
        private Label _lblTente = null!;
        private Label _lblGps = null!;
        private Label _lblZone = null!;
        private Label _lblPlayers = null!;
        private Label _lblClassementList = null!;
        private Label _lblBadges = null!;
        private Label _lblCampagneInfo = null!;
        private OptionButton _optJ1 = null!;
        private OptionButton _optJ2 = null!;
        private OptionButton _optCampType = null!;
        private SpinBox _editCampJours = null!;
        private LineEdit _editCampDebut = null!;
        private Label _lblCompteInfo = null!;
        private Control _superPanel = null!;
        private Control _chefPanel = null!;
        private Label _lblAttente = null!;
        private Label _lblDemandes = null!;
        private Label _lblDemandesActivites = null!;
        private Label _lblDemandesActivitesList = null!;
        private Label _lblUsers = null!;
        private Label _lblUsersList = null!;
        private LineEdit _editSaPseudo = null!;
        private LineEdit _editSaPass = null!;
        private OptionButton _optSaRole = null!;
        private LineEdit _editSaEquipe = null!;
        private LineEdit _editChefPseudo = null!;
        private LineEdit _editChefPass = null!;
        private Label _lblMesCreations = null!;
        private LineEdit _editPoiNom = null!;
        private LineEdit _editPoiLat = null!;
        private LineEdit _editPoiLon = null!;
        private OptionButton _optPoiInterdit = null!;
        private LineEdit _editPoiDesc = null!;
        private Label _lblPoiList = null!;
        private Label _lblActivitesTitre = null!;
        private HBoxContainer _jourBtns = null!;
        private Label _lblActivitesList = null!;
        private Label _lblEnigme = null!;
        private LineEdit _editReponse = null!;
        private Label _lblReponseMsg = null!;
        private Label _lblEssais = null!;
        private LineEdit _editMsgClair = null!;
        private OptionButton _optCodeChoix = null!;
        private LineEdit _editParam = null!;
        private Label _lblCodeResult = null!;
        private Label _lblCodeAide = null!;
        private LineEdit _editTekSearch = null!;
        private HBoxContainer _tekFiltres = null!;
        private Label _lblTekList = null!;
        private Label _lblQuizQ = null!;
        private HBoxContainer _quizOpts = null!;
        private Label _lblQuizMsg = null!;
        private Label _lblEquipesList = null!;
        private Label _lblPositions = null!;
        private Label _lblAlertes = null!;
        private Label _lblAudit = null!;
        private Label _lblCreerDroit = null!;
        private Label _lblCreerQuota = null!;
        private LineEdit _editNTitre = null!;
        private OptionButton _optNModele = null!;
        private OptionButton _optNCode = null!;
        private LineEdit _editNClair = null!;
        private SpinBox _editNPoints = null!;
        private SpinBox _editNJour = null!;
        private LineEdit _editNHeure = null!;
        private OptionButton _optNLieu = null!;
        private LineEdit _editNTeknika = null!;
        private LineEdit _editNReponses = null!;
        private LineEdit _editNIndice1 = null!;
        private LineEdit _editNIndice2 = null!;
        private LineEdit _editNIndice3 = null!;
        private SpinBox _editNEssais = null!;
        private OptionButton _optNVariante = null!;
        private OptionButton _optNPortee = null!;
        private OptionButton _optNTarikaCible = null!;
        private CheckBox _checkNEvenementCommun = null!;
        private Label _lblApercu = null!;
        private Label _lblBrouillons = null!;
        private Label _lblDemandesValidation = null!;
        private LineEdit _editBackendUrl = null!;
        private LineEdit _editCampagneId = null!;
        private LineEdit _editEquipeId = null!;
        private LineEdit _editPseudoBackend = null!;
        private Label _lblBackendTest = null!;
        private Label _lblQuest = null!;
        private Label _lblQuestList = null!;
        private Control _dialogPanel = null!;
        private Label _lblDialogNom = null!;
        private Label _lblDialogTxt = null!;
        private Button _btnInteract = null!;
        private Control _endScreen = null!;
        private Label _lblEndNom = null!;
        private Label _lblEndScore = null!;
        private Label _lblEndTotem = null!;
        private SubViewportContainer? _view3D;
        private CharacterBody3D? _j1;
        private CharacterBody3D? _j2;
        private MeshInstance3D? _fleche;
        private Camera3D? _cam;
        private Control? _joyGauche;
        private Control? _joyDroit;
        private string _panelActuel = "Camp";
        private Activite? _activiteJouee;
        private int _essaisActuels;
        private Teknika? _tekQuiz;
        private string _tekFiltre = "Rehetra / Tout";
        private int _jourFiltre = 1;
        private int _feu = 0;
        private int _eau = 0;
        private int _bois = 0;
        private int _tente = 0;
        private bool _isNight;
        private string _joueur1Pseudo = "";
        private string _joueur1Role = "";
        private string _joueur2Pseudo = "";
        private string _joueur2Role = "";
        private string _equipeChoisie = "";
        private string _backendUrl = "";
        private string _campagneIdBackend = "";
        private string _equipeIdBackend = "";

        public override void _Ready()
        {
            _gm = GameManager.Instance;
            _resp = ResponsiveManager.Instance;
            _gps = new GPS();
            AddChild(_gps);
            GetRefs();
            FillOptions();
            ConnectButtons();
            Build3D();
            BuildJoysticks();
            _resp.OnResolutionChangee += OnResolutionChangee;
            AppliquerResponsive(_resp.TypeActuel, _resp.OrientationActuelle, _resp.FacteurEchelle);
            ShowPanel("Camp");
            SwitchAuth("login");
            RefreshAll();
            SetStatus("Vonona / Prêt — " + _resp.TypeActuel + " " + _resp.OrientationActuelle);
        }

        private void GetRefs()
        {
            _status = GetNode<Label>("RootVBox/StatusLabel");
            _statusMini = GetNode<Label>("RootVBox/TopBar/StatusMini");
            _titre = GetNode<Label>("RootVBox/TopBar/TitreLabel");
            _loginForm = GetNode<GridContainer>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/LoginForm");
            _registerForm = GetNode<GridContainer>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm");
            _firstForm = GetNode<GridContainer>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/FirstForm");
            _editLoginPseudo = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/LoginForm/EditLoginPseudo");
            _editLoginPass = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/LoginForm/EditLoginPass");
            _editSecret = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/LoginForm/EditSecret");
            _editRegPseudo = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegPseudo");
            _editRegPass = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegPass");
            _optRegRole = GetNode<OptionButton>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/OptRegRole");
            _editRegEquipe = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegEquipe");
            _editRegRoleEq = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegRoleEq");
            _editRegParrain = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegParrain");
            _editRegParent = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegParent");
            _editRegContactParent = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/EditRegContactParent");
            _checkConsentement = GetNode<CheckBox>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/RegisterForm/CheckConsentement");
            _editFirstPseudo = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/FirstForm/EditFirstPseudo");
            _editFirstPass = GetNode<LineEdit>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/FirstForm/EditFirstPass");
            _lblAuthMsg = GetNode<Label>("RootVBox/ContentScroll/Content/AuthPanel/AuthVBox/LblAuthMsg");
            _lblPersoApercu = GetNode<Label>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoApercu");
            _editPersoNom = GetNode<LineEdit>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/EditPersoNom");
            _optPersoAvatar = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoAvatar");
            _optPersoTenue = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoTenue");
            _optPersoFoulard = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoFoulard");
            _optPersoArme = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoArme");
            _editPersoTaille = GetNode<SpinBox>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/EditPersoTaille");
            _optPersoGenre = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoGenre");
            _optPersoCheveux = GetNode<OptionButton>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/PersoForm/OptPersoCheveux");
            _lblPersoMsg = GetNode<Label>("RootVBox/ContentScroll/Content/PersoPanel/PersoVBox/LblPersoMsg");
            _lblTarikaList = GetNode<Label>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/LblTarikaList");
            _editTarikaNom = GetNode<LineEdit>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/TarikaForm/EditTarikaNom");
            _optTarikaChef = GetNode<OptionButton>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/TarikaForm/OptTarikaChef");
            _optTarikaEmbleme = GetNode<OptionButton>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/TarikaForm/OptTarikaEmbleme");
            _editTarikaDevise = GetNode<LineEdit>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/TarikaForm/EditTarikaDevise");
            _lblTarikaMsg = GetNode<Label>("RootVBox/ContentScroll/Content/TarikaPanel/TarikaVBox/LblTarikaMsg");
            _lblFeu = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampStats/LblFeu");
            _lblEau = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampStats/LblEau");
            _lblBois = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampStats/LblBois");
            _lblTente = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampStats/LblTente");
            _lblGps = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblGps");
            _lblZone = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblZone");
            _lblPlayers = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblPlayers");
            _lblClassementList = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblClassementList");
            _lblBadges = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblBadges");
            _lblCampagneInfo = GetNode<Label>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/LblCampagneInfo");
            _optJ1 = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/JoueursForm/OptJ1");
            _optJ2 = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/JoueursForm/OptJ2");
            _optCampType = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampagneForm/OptCampType");
            _editCampJours = GetNode<SpinBox>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampagneForm/EditCampJours");
            _editCampDebut = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CampPanel/CampVBox/CampagneForm/EditCampDebut");
            _lblCompteInfo = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/LblCompteInfo");
            _superPanel = GetNode<Control>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel");
            _chefPanel = GetNode<Control>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/ChefPanel");
            _lblAttente = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/LblAttente");
            _lblDemandes = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/LblDemandes");
            _lblDemandesActivites = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/LblDemandesActivites");
            _lblDemandesActivitesList = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/LblDemandesActivitesList");
            _lblUsers = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/LblUsers");
            _lblUsersList = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/LblUsersList");
            _editSaPseudo = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/SuperForm/EditSaPseudo");
            _editSaPass = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/SuperForm/EditSaPass");
            _optSaRole = GetNode<OptionButton>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/SuperForm/OptSaRole");
            _editSaEquipe = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/SuperPanel/SuperForm/EditSaEquipe");
            _editChefPseudo = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/ChefPanel/ChefForm/EditChefPseudo");
            _editChefPass = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/ChefPanel/ChefForm/EditChefPass");
            _lblMesCreations = GetNode<Label>("RootVBox/ContentScroll/Content/ComptesPanel/ComptesVBox/ChefPanel/LblMesCreations");
            _editPoiNom = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/PoiForm/EditPoiNom");
            _editPoiLat = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/PoiForm/EditPoiLat");
            _editPoiLon = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/PoiForm/EditPoiLon");
            _optPoiInterdit = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/PoiForm/OptPoiInterdit");
            _editPoiDesc = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/PoiForm/EditPoiDesc");
            _lblPoiList = GetNode<Label>("RootVBox/ContentScroll/Content/CartePanel/CarteVBox/LblPoiList");
            _lblActivitesTitre = GetNode<Label>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/ActivitesTitre");
            _jourBtns = GetNode<HBoxContainer>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/JourBtns");
            _lblActivitesList = GetNode<Label>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/LblActivitesList");
            _lblEnigme = GetNode<Label>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/LblEnigme");
            _editReponse = GetNode<LineEdit>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/EditReponse");
            _lblReponseMsg = GetNode<Label>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/LblReponseMsg");
            _lblEssais = GetNode<Label>("RootVBox/ContentScroll/Content/ActivitesPanel/ActivitesVBox/LblEssais");
            _editMsgClair = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CodesPanel/CodesVBox/CodesForm/EditMsgClair");
            _optCodeChoix = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CodesPanel/CodesVBox/CodesForm/OptCodeChoix");
            _editParam = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CodesPanel/CodesVBox/CodesForm/EditParam");
            _lblCodeResult = GetNode<Label>("RootVBox/ContentScroll/Content/CodesPanel/CodesVBox/LblCodeResult");
            _lblCodeAide = GetNode<Label>("RootVBox/ContentScroll/Content/CodesPanel/CodesVBox/LblCodeAide");
            _editTekSearch = GetNode<LineEdit>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/EditTekSearch");
            _tekFiltres = GetNode<HBoxContainer>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/TekFiltres");
            _lblTekList = GetNode<Label>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/LblTekList");
            _lblQuizQ = GetNode<Label>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/LblQuizQ");
            _quizOpts = GetNode<HBoxContainer>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/QuizOpts");
            _lblQuizMsg = GetNode<Label>("RootVBox/ContentScroll/Content/TeknikaPanel/TeknikaVBox/LblQuizMsg");
            _lblEquipesList = GetNode<Label>("RootVBox/ContentScroll/Content/EquipesPanel/EquipesVBox/LblEquipesList");
            _lblPositions = GetNode<Label>("RootVBox/ContentScroll/Content/ChefPanel/ChefVBox/LblPositions");
            _lblAlertes = GetNode<Label>("RootVBox/ContentScroll/Content/ChefPanel/ChefVBox/LblAlertes");
            _lblAudit = GetNode<Label>("RootVBox/ContentScroll/Content/ChefPanel/ChefVBox/LblAudit");
            _lblCreerDroit = GetNode<Label>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/LblCreerDroit");
            _lblCreerQuota = GetNode<Label>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/LblCreerQuota");
            _editNTitre = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNTitre");
            _optNModele = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNModele");
            _optNCode = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNCode");
            _editNClair = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNClair");
            _editNPoints = GetNode<SpinBox>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNPoints");
            _editNJour = GetNode<SpinBox>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNJour");
            _editNHeure = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNHeure");
            _optNLieu = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNLieu");
            _editNTeknika = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNTeknika");
            _editNReponses = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNReponses");
            _editNIndice1 = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNIndice1");
            _editNIndice2 = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNIndice2");
            _editNIndice3 = GetNode<LineEdit>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNIndice3");
            _editNEssais = GetNode<SpinBox>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/EditNEssais");
            _optNVariante = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNVariante");
            _optNPortee = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNPortee");
            _optNTarikaCible = GetNode<OptionButton>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/OptNTarikaCible");
            _checkNEvenementCommun = GetNode<CheckBox>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/CreerForm/CheckNEvenementCommun");
            _lblApercu = GetNode<Label>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/LblApercu");
            _lblBrouillons = GetNode<Label>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/LblBrouillons");
            _lblDemandesValidation = GetNode<Label>("RootVBox/ContentScroll/Content/CreerPanel/CreerVBox/LblDemandesValidation");
            _editBackendUrl = GetNode<LineEdit>("RootVBox/ContentScroll/Content/BackendPanel/BackendVBox/BackendForm/EditBackendUrl");
            _editCampagneId = GetNode<LineEdit>("RootVBox/ContentScroll/Content/BackendPanel/BackendVBox/BackendForm/EditCampagneId");
            _editEquipeId = GetNode<LineEdit>("RootVBox/ContentScroll/Content/BackendPanel/BackendVBox/BackendForm/EditEquipeId");
            _editPseudoBackend = GetNode<LineEdit>("RootVBox/ContentScroll/Content/BackendPanel/BackendVBox/BackendForm/EditPseudoBackend");
            _lblBackendTest = GetNode<Label>("RootVBox/ContentScroll/Content/BackendPanel/BackendVBox/LblBackendTest");
            _lblQuest = GetNode<Label>("RootVBox/ContentScroll/Content/Panel3D/VBox3D/LblQuest");
            _lblQuestList = GetNode<Label>("RootVBox/ContentScroll/Content/Panel3D/VBox3D/LblQuestList");
            _dialogPanel = GetNode<Control>("RootVBox/ContentScroll/Content/DialogPanel");
            _lblDialogNom = GetNode<Label>("RootVBox/ContentScroll/Content/DialogPanel/DialogVBox/LblDialogNom");
            _lblDialogTxt = GetNode<Label>("RootVBox/ContentScroll/Content/DialogPanel/DialogVBox/LblDialogTxt");
            _btnInteract = GetNode<Button>("RootVBox/ContentScroll/Content/BtnInteract");
            _endScreen = GetNode<Control>("EndScreen");
            _lblEndNom = GetNode<Label>("EndScreen/EndVBox/LblEndNom");
            _lblEndScore = GetNode<Label>("EndScreen/EndVBox/LblEndScore");
            _lblEndTotem = GetNode<Label>("EndScreen/EndVBox/LblEndTotem");
        }

        private void FillOptions()
        {
            _optRegRole.Clear(); _optRegRole.AddItem("🧑 Mpikambana / Membre", 0); _optRegRole.AddItem("🧭 Mpitarika / Chef", 1); _optRegRole.AddItem("🎭 Mpanentana / Animateur", 2);
            _optSaRole.Clear(); _optSaRole.AddItem("🧑 Mpikambana / Membre", 0); _optSaRole.AddItem("🧭 Mpitarika / Chef", 1); _optSaRole.AddItem("🎭 Mpanentana / Animateur", 2); _optSaRole.AddItem("👑 Mpitantana Ambony / Super Admin", 3);
            _optPoiInterdit.Clear(); _optPoiInterdit.AddItem("🟢 Avela / Autorisé", 0); _optPoiInterdit.AddItem("🔴 Voarara / Interdit (géofence)", 1);
            string[] codes = { "Cesar", "Morse", "Atbash", "Vigenere", "Polybe", "A1Z26", "Inverse", "Semaphore", "Pigpen", "Braille", "Signes de piste", "Morse lampe", "Morse sifflet", "OTAN", "Montre Murale", "Pyramide", "Code Q", "Chiffre", "Lohateny", "Code-Code", "Vakana", "Rantsan-kazo", "Arabie", "Voanjo", "Dan", "Fige", "Kely" };
            _optCodeChoix.Clear(); _optNCode.Clear(); foreach (string c in codes) { _optCodeChoix.AddItem(c); _optNCode.AddItem(c); }
            string[] modeles = { "enigme_codee", "chasse_tresor", "jeu_nuit", "veillee", "survie", "corvee", "observation", "secourisme", "grand_jeu", "koja_skoto", "vona", "teknika" };
            _optNModele.Clear(); foreach (string m in modeles) _optNModele.AddItem(m);
            _optNVariante.Clear(); _optNVariante.AddItem("An-tany / Terrain", 0); _optNVariante.AddItem("Virtoaly / Virtuel", 1); _optNVariante.AddItem("Mifangaro / Hybride", 2); _optNVariante.Select(2);
            _optNPortee.Clear();
            _optNPortee.AddItem("🏕️ Asa Tarika — Tarikako ihany / Mon groupe — Avy hatrany", 0);
            _optNPortee.AddItem("🌍 Asa Rehetra — Rehetra / Tous — Mangata + Super Admin + Quota 2/sem", 1);
            _optNPortee.AddItem("🔀 Cross-Tarika — Tarika hafa 1 / Un autre groupe — Mangata + Super Admin", 2);
            _optCampType.Clear();
            _optCampType.AddItem("⚡ Lasy Tselatra — 1 → 3 andro / jours", 0);
            _optCampType.AddItem("🏕️ Lasy Lehibe — 1 → 5 andro / jours", 1);
            _optCampType.AddItem("🌲 Lasy Feno — 1 → 11 andro / jours", 2);
            _optCampType.Select(0);
            _optPersoAvatar.Clear(); foreach (var e in new[] { "🧑", "👩", "🧔", "👱‍♀️", "🦸", "🧙", "🏹", "🗡️", "👑" }) _optPersoAvatar.AddItem(e);
            _optPersoTenue.Clear(); foreach (var t in new[] { "Tenue Skoto Classique", "Tenue Nuit", "Tenue Pluie", "Tenue Cérémonie", "Explorateur" }) _optPersoTenue.AddItem(t);
            _optPersoFoulard.Clear(); foreach (var f in new[] { "Rouge", "Vert", "Bleu", "Jaune", "Orange" }) _optPersoFoulard.AddItem(f);
            _optPersoArme.Clear(); foreach (var a in new[] { "Bâton", "Corde", "Lampe", "Boussole", "Sac" }) _optPersoArme.AddItem(a);
            _optPersoGenre.Clear(); foreach (var g in new[] { "Non précisé", "Garçon", "Fille" }) _optPersoGenre.AddItem(g);
            _optPersoCheveux.Clear(); foreach (var c in new[] { "Court", "Long", "Tressé", "Rasé", "Natte" }) _optPersoCheveux.AddItem(c);
            _optTarikaEmbleme.Clear(); foreach (var e in new[] { "🦅", "🦁", "🐢", "🐘", "🌳", "🔥", "⭐", "🏔️" }) _optTarikaEmbleme.AddItem(e);
            RefreshLieuxOptions(); RefreshJoueursOptions(); RefreshTarikaChefs(); RefreshTarikaCibleOptions();
            SyncCampagneJoursMax();
        }

        private void RefreshLieuxOptions()
        {
            _optNLieu.Clear();
            if (_gm.Data.Pois.Count == 0) { _optNLieu.AddItem("Tsy misy toerana — mamorona aloha / Aucun lieu — crée-en d’abord"); return; }
            foreach (Poi p in _gm.Data.Pois) _optNLieu.AddItem(p.Nom);
        }

        private void RefreshJoueursOptions()
        {
            _optJ1.Clear(); _optJ2.Clear();
            var membres = new List<User>();
            foreach (User u in _gm.Data.Users) if (u.Role == Role.Membre && u.Statut == StatutCompte.Valide) membres.Add(u);
            if (membres.Count == 0) { _optJ1.AddItem("Tsy misy mpikambana voamarina / Aucun membre validé"); _optJ2.AddItem("Tsy misy mpikambana voamarina / Aucun membre validé"); return; }
            foreach (User m in membres) { var p = _gm.GetPerso(m.Pseudo); string label = p.AvatarEmoji + " " + p.NomPersonnage + " (" + m.Pseudo + ") - " + m.Equipe + " - " + m.RoleEquipe; _optJ1.AddItem(label); _optJ2.AddItem(label); }
        }

        private void RefreshTarikaChefs()
        {
            _optTarikaChef.Clear();
            foreach (User u in _gm.Data.Users) if (u.Role == Role.Chef && u.Statut == StatutCompte.Valide) _optTarikaChef.AddItem(u.Pseudo);
            if (_optTarikaChef.ItemCount == 0) _optTarikaChef.AddItem("Tsy misy mpitarika voamarina / Aucun chef validé");
        }

        private void RefreshTarikaCibleOptions()
        {
            _optNTarikaCible.Clear();
            if (_gm.Data.Tarikas.Count == 0) { _optNTarikaCible.AddItem("Tsy misy tarika / Aucun groupe"); return; }
            foreach (Tarika t in _gm.Data.Tarikas) _optNTarikaCible.AddItem(t.Embleme + " " + t.Nom + " — " + t.ChefPseudo);
        }

        private void ConnectButtons()
        {
            GetNode<Button>("RootVBox/TopBar/BtnDeconnexion").Pressed += OnLogout;
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabCamp").Pressed += () => ShowPanel("Camp");
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabPerso").Pressed += () => ShowPanel("Perso");
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabTarika").Pressed += () => ShowPanel("Tarika");
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabComptes").Pressed += () => ShowPanel("Comptes");
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabCarte").Pressed += () => ShowPanel("Carte");
            GetNode<Button>("RootVBox/TabScroll/TabBar/BtnTabActivites").Pressed += () => ShowPanel("Activites");
            GetNode<Button>("RootVBox/Tab