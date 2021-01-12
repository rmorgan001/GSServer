namespace NStarAlignment.DataTypes
{
    public class AlignmentStar
    {
        public string CommonName { get; set; }
        public string AlternateName { get; set; }
        public double Ra { get; set; }
        public double Dec { get; set; }

        public double Mag { get; set; }

        public AlignmentStar(string commonName, string alternateName, double ra, double dec, double mag)
        {
            CommonName = commonName;
            AlternateName = alternateName;
            Ra = ra;
            Dec = dec;
            Mag = mag;
        }
    }
}
