using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sahavola
{
    public partial class GPS : Node
    {
        private const int TailleFiltre = 5;
        private const double PrecisionMaxMetres = 25.0;
        private const double RayonAlerteMetres = 50.0;
        private const double DureeAlerteSecondes = 8.0;

        private readonly Queue<(double Lat, double Lon)> _historique = new();
        private readonly HashSet<string> _alertesActives = new();

        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public double Precision { get; private set; }
        public bool Actif { get; private set; }

        public void MettreAJourPosition(double lat, double lon, double precisionMetres, bool fauxFournisseur)
        {
            if (fauxFournisseur)
            {
                GameManager.Instance?.SignalerAnomalieGps("fournisseur_simule");
                return;
            }
            if (double.IsNaN(lat) || double.IsNaN(lon) || precisionMetres > PrecisionMaxMetres) return;

            _historique.Enqueue((lat, lon));
            if (_historique.Count > TailleFiltre) _historique.Dequeue();

            Lat = _historique.Average(p => p.Lat);
            Lon = _historique.Average(p => p.Lon);
            Precision = precisionMetres;
            Actif = true;
            VerifierZones();
        }

        private void VerifierZones()
        {
            GameManager? gm = GameManager.Instance;
            if (gm == null) return;

            foreach (Poi zone in gm.ZonesInterdites())
            {
                double distance = Geo.Haversine(Lat, Lon, zone.Lat, zone.Lon);
                if (distance >= RayonAlerteMetres || !_alertesActives.Add(zone.Nom)) continue;

                gm.SignalerAlerte(zone.Nom, distance);
                string nom = zone.Nom;
                GetTree().CreateTimer(DureeAlerteSecondes).Timeout += () => { _alertesActives.Remove(nom); };
            }
        }

        public bool EnvoyerSos(string equipe)
        {
            GameManager? gm = GameManager.Instance;
            if (gm == null || !Actif) return false;
            gm.EnregistrerSos(equipe, Lat, Lon);
            return true;
        }
    }
}
