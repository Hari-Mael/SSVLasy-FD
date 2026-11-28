using Godot;
using System;

namespace Sahavola
{
    // ResponsiveManager — Foiben'ny résolution rehetra
    // Gestionnaire central de la résolution — un seul endroit, pas de duplication
    // Téléphone / Tablette / Ordinateur — détecté automatiquement

    public enum TypeEcran { Telephone, Tablette, Ordinateur, TeleGrand }
    public enum OrientationEcran { Portrait, Paysage }

    public partial class ResponsiveManager : Node
    {
        public static ResponsiveManager Instance { get; private set; } = null!;

        public TypeEcran TypeActuel { get; private set; } = TypeEcran.Ordinateur;
        public OrientationEcran OrientationActuelle { get; private set; } = OrientationEcran.Paysage;
        public Vector2I TailleEcran { get; private set; } = new Vector2I(1280, 720);
        public float FacteurEchelle { get; private set; } = 1.0f;
        public bool EstTactile { get; private set; }
        public bool EstPetitEcran => TypeActuel == TypeEcran.Telephone;

        // Événement — ny UI rehetra dia mihaino ity ihany
        // Événement — toute l'UI écoute celui-ci
        public event Action<TypeEcran, OrientationEcran, float>? OnResolutionChangee;

        public override void _Ready()
        {
            Instance = this;
            // Détection tactile : Android / iOS = tactile, Windows / Linux / macOS = souris
            EstTactile = OS.GetName() == "Android" || OS.GetName() == "iOS" || DisplayServer.IsTouchscreenAvailable();
            GetViewport().SizeChanged += OnTailleChangee;
            OnTailleChangee();
        }

        private void OnTailleChangee()
        {
            Vector2I taille = (Vector2I)GetViewport().GetVisibleRect().Size;
            // Sécurité : raha 0, mampiasa 1280x720
            if (taille.X <= 0 || taille.Y <= 0) taille = new Vector2I(1280, 720);
            TailleEcran = taille;

            // Orientation
            OrientationActuelle = taille.X < taille.Y ? OrientationEcran.Portrait : OrientationEcran.Paysage;

            // Type écran — araka ny sakany kely indrindra (smallest width)
            // Type écran — selon la plus petite largeur
            int petitCote = Math.Min(taille.X, taille.Y);
            int grandCote = Math.Max(taille.X, taille.Y);

            if (petitCote < 600)
            {
                TypeActuel = TypeEcran.Telephone;
                // Téléphone : soratra kely kokoa fa bokotra lehibe (rantsan-tanana)
                FacteurEchelle = 0.85f;
            }
            else if (petitCote < 900)
            {
                TypeActuel = TypeEcran.Tablette;
                FacteurEchelle = 1.0f;
            }
            else if (grandCote >= 1920)
            {
                TypeActuel = TypeEcran.TeleGrand;
                FacteurEchelle = 1.3f;
            }
            else
            {
                TypeActuel = TypeEcran.Ordinateur;
                FacteurEchelle = 1.1f;
            }

            // Portrait téléphone : facteur kely kokoa mba hidiran'ny zava-drehetra
            if (TypeActuel == TypeEcran.Telephone && OrientationActuelle == OrientationEcran.Portrait)
            {
                FacteurEchelle = 0.8f;
            }

            GD.Print($"[Responsive] {TypeActuel} {OrientationActuelle} {taille.X}x{taille.Y} echelle={FacteurEchelle} tactile={EstTactile}");
            OnResolutionChangee?.Invoke(TypeActuel, OrientationActuelle, FacteurEchelle);
        }

        // ---------- Helpers — ampiasaina amin'ny UI rehetra ----------
        public int TaillePoliceTitre() => TypeActuel switch
        {
            TypeEcran.Telephone => 18,
            TypeEcran.Tablette => 22,
            TypeEcran.TeleGrand => 32,
            _ => 26
        };

        public int TaillePoliceTexte() => TypeActuel switch
        {
            TypeEcran.Telephone => 13,
            TypeEcran.Tablette => 15,
            TypeEcran.TeleGrand => 20,
            _ => 16
        };

        public int HauteurBouton() => TypeActuel switch
        {
            TypeEcran.Telephone => 52, // Rantsan-tanana : 48px farafahakeliny / Doigt : 48px minimum
            TypeEcran.Tablette => 56,
            TypeEcran.TeleGrand => 64,
            _ => 48
        };

        public int ColonnesGrille() => (TypeActuel, OrientationActuelle) switch
        {
            (TypeEcran.Telephone, OrientationEcran.Portrait) => 1,
            (TypeEcran.Telephone, OrientationEcran.Paysage) => 2,
            (TypeEcran.Tablette, OrientationEcran.Portrait) => 2,
            (TypeEcran.Tablette, OrientationEcran.Paysage) => 3,
            (TypeEcran.TeleGrand, _) => 4,
            _ => 3
        };

        public bool FautAfficherJoystick() => EstTactile || TypeActuel == TypeEcran.Telephone || TypeActuel == TypeEcran.Tablette;

        public bool FautAfficherClavierVirtuel() => EstTactile;

        // Safe Area — notch / encoche — tsy asiana bokotra ao
        // Zone sûre — notch — pas de boutons dedans
        public Rect2I ZoneSure()
        {
            Rect2I safe = DisplayServer.GetDisplaySafeArea();
            if (safe.Size.X <= 0) safe = new Rect2I(Vector2I.Zero, TailleEcran);
            return safe;
        }

        // Adapter un Control — fomba tokana
        public void AdapterBouton(Button btn)
        {
            if (btn == null) return;
            btn.CustomMinimumSize = new Vector2(0, HauteurBouton());
            var font = btn.GetThemeFont("font");
            if (font != null) btn.AddThemeFontSizeOverride("font_size", TaillePoliceTexte());
        }

        public void AdapterLabel(Label lbl, bool estTitre = false)
        {
            if (lbl == null) return;
            lbl.AddThemeFontSizeOverride("font_size", estTitre ? TaillePoliceTitre() : TaillePoliceTexte());
            lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        }
    }
}
