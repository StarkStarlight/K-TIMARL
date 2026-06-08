 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class BoatsSimulate : MonoBehaviour
{
    public List<FleetController> fleetList;
    public List<BoatController> boats;

    [Header("Distance Monitoring")]
    [SerializeField] private bool enableDistanceMonitoring = true;
    [SerializeField] private float monitoringInterval = 60f;
    [SerializeField] public List<Vector3> Destinations;
    private float monitoringTimer = 0f;

    void Start()
    {
        for (int i = 0; i < Destinations.Count; i++)
        {
            fleetList[i].SetFinalDestination(Destinations[i]);
        }
    }

    void Update()
    {
        if (enableDistanceMonitoring)
        {
            monitoringTimer -= Time.deltaTime;
            if (monitoringTimer <= 0f)
            {
                MonitorShipDistances();
                monitoringTimer = monitoringInterval;
            }
        }
    }

    public void MonitorShipDistances()
    {
        if (boats.Count < 2) return;

        Dictionary<BoatController, float> closestDistances = new Dictionary<BoatController, float>();
        Dictionary<BoatController, BoatController> closestBoats = new Dictionary<BoatController, BoatController>();

        foreach (var boat in boats)
        {
            if (boat == null) continue;
            float miniDistance = float.MaxValue;
            BoatController closestBoat = null;

            foreach (var otherBoat in boats)
            {
                if (otherBoat == null || otherBoat == boat) continue;
                float distance = Vector3.Distance(boat.transform.position, otherBoat.transform.position);
                if (distance < miniDistance)
                {
                    miniDistance = distance;
                    closestBoat = otherBoat;
                }
            }
            closestDistances[boat] = miniDistance;
            closestBoats[boat] = closestBoat;
        }

        //Debug.Log("========= 船只最短距离 =========");
        //Debug.Log($"检测时间: {System.DateTime.Now:T}");
        //Debug.Log($"船只总数: {boats.Count}");

        foreach (var pair in closestDistances.OrderBy(x => x.Value))
        {
            //Debug.Log($"{pair.Key.name} 的最短距离: {pair.Value:F2}, 最近的船是: {(closestBoats.TryGetValue(pair.Key, out BoatController b) ? b : null).name}");
        }
        //Debug.Log("===============================");
    }
}
