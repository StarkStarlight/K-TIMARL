using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCase3Coordinates : MonoBehaviour
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
        {"buyo_Green", new BLcoordinate[] { new BLcoordinate(-600, -200), new BLcoordinate(-300, -200), new BLcoordinate(0, -200), new BLcoordinate(300, -200),
                                             }
        },
        {"buyo_Red", new BLcoordinate[]   { new BLcoordinate(-600, 200), new BLcoordinate(-300, 200), new BLcoordinate(0, 200), new BLcoordinate(300, 200),
                                             }
        }
    };

    public GameObject buyo_Green;
    public GameObject buyo_Red;
    public Transform parent;
    List<GameObject> buyos = new List<GameObject>();

    void Start()
    {
        double originLat = 0;
        double originLon = 0;
        GeoCoordinateConverter(originLat, originLon);
        GenerateBuyo();
    }

    void GenerateBuyo()
    {
        foreach (KeyValuePair<string, BLcoordinate[]> kvp in BuyoCoordinates)
        {
            string key = kvp.Key;
            BLcoordinate[] value = kvp.Value;
            for (int i = 0; i < value.GetLongLength(0); i++)
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

        return new Vector3((float)(coordinate.getL() - originLongitude), 0, (float)(coordinate.getB() - originLatitude));
    }

    private static double DegreeToRadian(double degree)
    {
        return degree * Mathf.PI / 180.0;
    }
}