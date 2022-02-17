/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using NStarAlignment.DataTypes;
using System.Collections.Generic;

namespace NStarAlignment.Model
{
    public partial class AlignmentModel
    {
        public List<AlignmentStar> AlignmentStars { get; } = new List<AlignmentStar>()
        {
            new AlignmentStar("Acamar","",2.971013,-40.3047347222222,3.22),
            new AlignmentStar("Achernar","",1.62856763888889,-57.23675,0.54),
            new AlignmentStar("Acrux","",12.4433041388889,-63.0990472222222,1.28),
            new AlignmentStar("Adara","Adhara",6.97709605555556,-28.9720880555556,1.53),
            new AlignmentStar("Albireo","",19.5120244166667,27.9596713888889,3.08),
            new AlignmentStar("Alcor","",13.4204269722222,54.9879483333333,4),
            new AlignmentStar("Alcyone","",3.79140986111111,24.1051397222222,2.88),
            new AlignmentStar("Aldebaran","",4.59867825,16.50929,0.99),
            new AlignmentStar("Alderamin","",21.3096593611111,62.5855741666667,2.47),
            new AlignmentStar("Algenib","",0.220597666666667,15.1835925,2.84),
            new AlignmentStar("Algieba","",10.3328151666667,19.8419236111111,2.23),
            new AlignmentStar("Algol","",3.13614230555556,40.9556947222222,2.11),
            new AlignmentStar("Alhena","",6.62853563888889,16.3992725,2.02),
            new AlignmentStar("Alioth","",12.90048525,55.959825,1.76),
            new AlignmentStar("Alkaid","",13.7923440277778,49.3132522222222,1.86),
            new AlignmentStar("Almaak","",2.06498752777778,42.3297313888889,2.17),
            new AlignmentStar("Alnair","",22.1372189166667,-46.9610511111111,1.77),
            new AlignmentStar("Alnath","",5.43819894444444,28.607455,1.68),
            new AlignmentStar("Alnilam","",5.60355880555556,-1.20192333333333,1.72),
            new AlignmentStar("Alnitak","",5.67931230555556,-1.94257694444444,1.9),
            new AlignmentStar("AlphaCentauri","Rigil Kentaurus",14.6601388888889,-60.8338888888889,0),
            new AlignmentStar("Alphard","",9.45978980555556,-8.65860277777778,1.99),
            new AlignmentStar("Alphekka","Alphecca",15.5781313333333,26.7146866666667,2.22),
            new AlignmentStar("Alpheratz","",0.139793388888889,29.0904280555556,2.06),
            new AlignmentStar("Alshain","",19.9218879722222,6.406775,3.72),
            new AlignmentStar("Altair","",19.8463887222222,8.86832777777778,0.93),
            new AlignmentStar("Aludra","",7.40158333333333,-29.3030555555556,2.45),
            new AlignmentStar("Ankaa","",0.438060888888889,-42.3061025,2.4),
            new AlignmentStar("Antares","",16.4901273888889,-26.4320036111111,1.07),
            new AlignmentStar("Arcturus","",14.2610193888889,19.1824183333333,0.16),
            new AlignmentStar("Arneb","",5.54550466666667,-17.8222830555556,2.59),
            new AlignmentStar("Bellatrix","",5.41885036111111,6.3497025,1.66),
            new AlignmentStar("Betelgeuse","",5.91952802777778,7.40706111111111,0.57),
            new AlignmentStar("Canopus","",6.39919402777778,-52.6956083333333,-0.63),
            new AlignmentStar("Capella","",5.27815530555556,45.9979911111111,0.08),
            new AlignmentStar("Castor","",7.5767075,31.8885625,1.58),
            new AlignmentStar("Cor Caroli","",12.9337960555556,38.3183838888889,2.89),
            new AlignmentStar("Deneb","Alpha Cygni",20.6905315833333,45.2803383333333,1.33),
            new AlignmentStar("DenebKaitos","Beta Ceti",0.7265,-17.9866666666667,2.02),
            new AlignmentStar("Denebola","",11.8176655833333,14.5720422222222,2.13),
            new AlignmentStar("Diphda","",0.726491638888889,-17.9865775,2.05),
            new AlignmentStar("Dschubba","",16.0055555555556,-22.6216666666667,2.31),
            new AlignmentStar("Dubhe","",11.0621294166667,61.7510258333333,1.82),
            new AlignmentStar("Elnath","",5.43819444444444,28.6075,1.65),
            new AlignmentStar("Enif","",21.7364316388889,9.87501222222222,2.39),
            new AlignmentStar("Etamin","",17.9434361666667,51.4888883333333,2.23),
            new AlignmentStar("Fomalhaut","",22.9608460833333,-29.622235,1.23),
            new AlignmentStar("Gienah","",20.7701944444444,33.9702777777778,2.48),
            new AlignmentStar("Hadar","",14.0637218611111,-60.3730572222222,0.64),
            new AlignmentStar("Hamal","",2.11955627777778,23.4624191666667,2.02),
            new AlignmentStar("Izar","",14.7497838611111,27.0742138888889,2.5),
            new AlignmentStar("Kaus Australis","",18.4028653888889,-34.3846122222222,1.81),
            new AlignmentStar("Kocab","",14.8450911388889,74.1554994444444,2.06),
            new AlignmentStar("Markab","",23.0793480833333,15.2052630555556,2.49),
            new AlignmentStar("Megrez","",12.2570991944444,57.03262,3.3),
            new AlignmentStar("Menkar","",3.03799258333333,4.08974805555556,2.55),
            new AlignmentStar("Menkent","",14.1113888888889,-36.37,2.06),
            new AlignmentStar("Merak","",11.0306888611111,56.382425,2.35),
            new AlignmentStar("Mintaka","",5.53344444444445,-0.299093888888889,2.23),
            new AlignmentStar("Mira","",2.32244088888889,-2.97764833333333,6.54),
            new AlignmentStar("Mirach","",1.16220122222222,35.6205211111111,2.08),
            new AlignmentStar("Mirphak","",3.40538147222222,49.8611833333333,1.81),
            new AlignmentStar("Mirzam","",6.37833333333333,-17.9558333333333,1.95),
            new AlignmentStar("Mizar","",13.3987601944444,54.9253530555556,2.22),
            new AlignmentStar("Nihal","",5.47075652777778,-20.7594447222222,2.84),
            new AlignmentStar("Nunki","",18.9210913333333,-26.2967444444444,2.07),
            new AlignmentStar("Phad","",11.8971746944444,53.6947061111111,2.43),
            new AlignmentStar("Polaris","",2.53031811111111,89.2641033333333,2),
            new AlignmentStar("Pollux","",7.75526202777778,28.0262286111111,1.22),
            new AlignmentStar("Procyon","",7.65503286111111,5.22499333333333,0.4),
            new AlignmentStar("Rasalgethi","",17.2441278888889,14.3902444444444,3.37),
            new AlignmentStar("Rasalhague","",17.5822423333333,12.5600275,2.09),
            new AlignmentStar("Regulus","",10.1395313055556,11.9672097222222,1.41),
            new AlignmentStar("Rigel","",5.24229722222222,-8.20164333333333,0.28),
            new AlignmentStar("Sabik","",17.1729722222222,-15.7247222222222,2.43),
            new AlignmentStar("Sadalmelik","",22.0963996111111,-0.319851388888889,2.94),
            new AlignmentStar("Saiph","",5.79593777777778,-9.66966083333333,2.06),
            new AlignmentStar("Scheat","",23.06290525,28.0827891666667,2.47),
            new AlignmentStar("Shaula","",17.5601442222222,-37.1038283333333,1.63),
            new AlignmentStar("Shedir","",0.675122305555556,56.5373283333333,2.25),
            new AlignmentStar("Sirius","",6.75248425,-16.7160311111111,-1.44),
            new AlignmentStar("Spica","",13.4198823611111,-11.1613205555556,1.06),
            new AlignmentStar("Tarazed","",19.7709946388889,10.6132658333333,2.71),
            new AlignmentStar("Thuban","",14.0731536388889,64.3758572222222,3.65),
            new AlignmentStar("Unukalhai","",15.7377983888889,6.42562805555556,2.63),
            new AlignmentStar("Vega","",18.615649,38.7836916666667,0.03),
            new AlignmentStar("Vindemiatrix","",13.0362774166667,10.9591322222222,2.84),
            new AlignmentStar("Wezen","",7.13986111111111,-26.3933333333333,1.82),
            new AlignmentStar("Zosma","",11.2351388888889,20.5236111111111,2.56)
        };
    }
}
