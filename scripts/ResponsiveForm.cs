using Godot;

namespace Sahavola
{
    // Formulaire responsive — 1 colonne téléphone, 2 colonnes tablette / ordinateur
    // Formulaire responsive — 1 colonne téléphone, 2 colonnes tablette / ordinateur
    public partial class ResponsiveForm : GridContainer
    {
        private ResponsiveManager _resp = null!;

        public override void _Ready()
        {
            _resp = ResponsiveManager.Instance;
            _resp.OnResolutionChangee += OnResolutionChangee;
            Appliquer(_resp.TypeActuel, _resp.OrientationActuelle);
        }

        public override void _ExitTree()
        {
            _resp.OnResolutionChangee -= OnResolutionChangee;
        }

        private void OnResolutionChangee(TypeEcran type, OrientationEcran orient, float echelle)
        {
            Appliquer(type, orient);
        }

        private void Appliquer(TypeEcran type, OrientationEcran orient)
        {
            // Téléphone portrait : 1 colonne — mora tsindrina
            // Tablette / Ordinateur : 2 colonnes — mitsitsy toerana
            Columns = (type == TypeEcran.Telephone && orient == OrientationEcran.Portrait) ? 1 : 2;

            foreach (Node child in GetChildren())
            {
                if (child is Button btn)
                {
                    _resp.AdapterBouton(btn);
                }
                else if (child is Label lbl)
                {
                    _resp.AdapterLabel(lbl, false);
                }
                else if (child is LineEdit le)
                {
                    le.CustomMinimumSize = new Vector2(0, _resp.HauteurBouton());
                    le.AddThemeFontSizeOverride("font_size", _resp.TaillePoliceTexte());
                }
            }
        }
    }
}
