using System;

namespace Sahavola
{
    public static class Geo
    {
        public const double LatitudeOrigine = -18.9100;
        public const double LongitudeOrigine = 47.5350;
        private const double RayonTerreMetres = 6_371_000.0;
        private const double MetresParDegreLatitude = 110_574.0;
        private const double MetresParDegreLongitudeEquateur = 111_320.0;

        public static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = Radians(lat2 - lat1);
            double dLon = Radians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * RayonTerreMetres * Math.Asin(Math.Sqrt(a));
        }

        public static Vector3Godot VersMonde(double lat, double lon)
        {
            double metresParDegreLongitude = MetresParDegreLongitudeEquateur * Math.Cos(Radians(LatitudeOrigine));
            float x = (float)((lon - LongitudeOrigine) * metresParDegreLongitude);
            float z = (float)(-(lat - LatitudeOrigine) * MetresParDegreLatitude);
            return new Vector3Godot(x, 0f, z);
        }

        private static double Radians(double degres) => degres * Math.PI / 180.0;
    }
}
