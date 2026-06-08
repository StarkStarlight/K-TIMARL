using System.Collections;
using System.Collections.Generic;
using UnityEngine;  

public class BuyoCoordinate : MonoBehaviour
{

    private const double EarthRadius = 6378137.0;

    private double originLatitude;
    private double originLongitude;

    public class BLcoordinate
    {
        double B;
        double L;
        public BLcoordinate(double Breadth, double Length)
        {
            B = Breadth;
            L = Length;
        }

        public double getB()
        {
            return B;
        }

        public double getL()
        {
            return L;
        }

    }


    public void GeoCoordinateConverter(double originLat, double originLon)
    {
        originLatitude = originLat;
        originLongitude = originLon;
    }

    private static double C2DD(int degrees, double minutes)
    {
        return degrees + (minutes / 60);
    }

    public static Dictionary<string, GameObject> buyoType;
    public static readonly Dictionary<string, BLcoordinate[]> BuyoCoordinates = new Dictionary<string, BLcoordinate[]> {
        {"buyo_Green", new BLcoordinate[] { new BLcoordinate(C2DD(37, 35.618), C2DD(121, 22.748)), new BLcoordinate(C2DD(37, 35.550), C2DD(121, 23.111)), new BLcoordinate(C2DD(37, 35.442), C2DD(121, 22.634)),
                                            new BLcoordinate(C2DD(37, 35.307), C2DD(121, 23.731)), new BLcoordinate(C2DD(37, 35.008), C2DD(121, 23.990)), new BLcoordinate(C2DD(37, 34.898), C2DD(121, 23.796)),
                                            new BLcoordinate(C2DD(37, 34.815), C2DD(121, 24.547)), new BLcoordinate(C2DD(37, 34.831), C2DD(121, 25.295)), new BLcoordinate(C2DD(37, 35.082), C2DD(121, 26.115)),
                                            new BLcoordinate(C2DD(37, 34.373), C2DD(121, 24.328)), new BLcoordinate(C2DD(37, 34.143), C2DD(121, 23.866)), new BLcoordinate(C2DD(37, 34.072), C2DD(121, 23.552)),
                                            new BLcoordinate(C2DD(37, 33.111), C2DD(121, 23.740)), new BLcoordinate(C2DD(37, 33.065), C2DD(121, 24.763)), new BLcoordinate(C2DD(37, 33.362), C2DD(121, 25.612)) }
        },
        {"buyo_Red", new BLcoordinate[]   { new BLcoordinate(C2DD(37, 35.305), C2DD(121, 22.835)), new BLcoordinate(C2DD(37, 35.346), C2DD(121, 23.369)), new BLcoordinate(C2DD(37, 35.282), C2DD(121, 23.541)),
                                            new BLcoordinate(C2DD(37, 35.177), C2DD(121, 23.698)), new BLcoordinate(C2DD(37, 35.632), C2DD(121, 27.014)), new BLcoordinate(C2DD(37, 34.741), C2DD(121, 23.964)),
                                            new BLcoordinate(C2DD(37, 34.589), C2DD(121, 25.084)), new BLcoordinate(C2DD(37, 34.882), C2DD(121, 25.980)), new BLcoordinate(C2DD(37, 35.165), C2DD(121, 26.830)),
                                            new BLcoordinate(C2DD(37, 34.245), C2DD(121, 24.393)), new BLcoordinate(C2DD(37, 33.900), C2DD(121, 23.702)), new BLcoordinate(C2DD(37, 33.741), C2DD(121, 23.345)),
                                            new BLcoordinate(C2DD(37, 32.873), C2DD(121, 23.264)), new BLcoordinate(C2DD(37, 33.053), C2DD(121, 23.671)), new BLcoordinate(C2DD(37, 32.933), C2DD(121, 24.535)) }
        },
        {"buyo_North", new BLcoordinate[] { new BLcoordinate(C2DD(37, 35.632), C2DD(121, 23.125)), new BLcoordinate(C2DD(37, 35.402), C2DD(121, 23.024)) }
        },
        {"buyo_East", new BLcoordinate[]  { new BLcoordinate(C2DD(37, 35.648), C2DD(121, 22.969)), new BLcoordinate(C2DD(37, 35.581), C2DD(121, 23.369)), new BLcoordinate(C2DD(37, 34.882), C2DD(121, 23.914)),
                                            new BLcoordinate(C2DD(37, 34.642), C2DD(121, 24.746)) }
        },
        {"buyo_South", new BLcoordinate[] { new BLcoordinate(C2DD(37, 35.791), C2DD(121, 23.099)), }
        },
        {"buyo_West", new BLcoordinate[]  { new BLcoordinate(C2DD(37, 35.551), C2DD(121, 24.384)), new BLcoordinate(C2DD(37, 35.069), C2DD(121, 24.237)), new BLcoordinate(C2DD(37, 34.868), C2DD(121, 24.381)) }
        },
        {"buyo_Origin", new BLcoordinate[] { new BLcoordinate(C2DD(37, 34.637), C2DD(121, 23.522)), new BLcoordinate(C2DD(37, 34.082), C2DD(121, 23.522)), new BLcoordinate(C2DD(37, 35.185), C2DD(121, 26.826)) } }
    };

    public GameObject buyo_Green;
    public GameObject buyo_Red;
    public GameObject buyo_North;
    public GameObject buyo_East;
    public GameObject buyo_South;
    public GameObject buyo_West;
    public GameObject buyo_Restricted;
    public Transform parent;
    List<GameObject> buyos = new List<GameObject>();

    void Start()
    {
        double originLat = C2DD(37, 34.637);
        double originLon = C2DD(121, 23.522);
        GeoCoordinateConverter(originLat, originLon);
        GenerateBuyo();
    }

    void GenerateBuyo()
    {
        foreach (KeyValuePair<string, BLcoordinate[]> kvp in BuyoCoordinates)
        {
            string key = kvp.Key;
            BLcoordinate[] value = kvp.Value;
            for (int i=0; i < value.GetLongLength(0); i++)
            {
                GameObject buyo_Type = null;
                if (key == "buyo_Green")
                {
                    buyo_Type = buyo_Green;
                } 
                else if (key == "buyo_Red")
                {
                    buyo_Type = buyo_Red;
                } 
                else if (key == "buyo_North")
                {
                    buyo_Type = buyo_North;
                } 
                else if (key == "buyo_East")
                {
                    buyo_Type = buyo_East;
                }
                else if (key == "buyo_South")
                {
                    buyo_Type = buyo_South;
                }
                else if (key == "buyo_West")
                {
                    buyo_Type = buyo_West;
                }
                else if (key == "buyo_Origin")
                {
                    buyo_Type = buyo_Restricted;
                }
                GameObject buyo = Instantiate(buyo_Type, BL2V3(value[i]), Quaternion.identity, parent);
                buyos.Add(buyo);
            }
        }
    }
    
    Vector3 BL2V3(BLcoordinate coordinate)
    {
        double dLon = DegreeToRadian(coordinate.getL() - originLongitude);
        double dLat = DegreeToRadian(coordinate.getB() - originLatitude);

        double x = EarthRadius * dLon * Mathf.Cos((float)DegreeToRadian(originLatitude));
        double y = EarthRadius * dLat;

        return new Vector3((float)x, 0, (float)y);
    }

    private static double DegreeToRadian(double degree)
    {
        return degree * Mathf.PI / 180.0;
    }

}
