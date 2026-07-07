using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RouteListController : MonoBehaviour
{
    [SerializeField] private GameObject routeCardPrefab;
    [SerializeField] private Transform contentParent;

    void Start()
    {
        Debug.Log("RouteListController Start ejecutado");
        BuildHardcodedRoutes();
        Debug.Log("Cards instanciadas: " + contentParent.childCount);
    }

    void BuildHardcodedRoutes()
    {
        var routes = new List<RouteData>
        {
            new RouteData
            {
                nombre = "Los caminos del agua",
                descripcionCorta = "Recorre el patrimonio hídrico del Centro Histórico de Bogotá",
                duracionMin = 150,
                distanciaM = 1800f,
                numPOIs = 8,
                dificultad = "Fácil",
                imagenUrl = "",
                poiServiceUrl = "https://services.arcgis.com/8DAUcrpQcpyLMznu/arcgis/rest/services/POIs_CaminosPatrimoniales/FeatureServer",
                rutaServiceUrl = "https://services.arcgis.com/8DAUcrpQcpyLMznu/arcgis/rest/services/Recorrido_CaminosPatrimoniales/FeatureServer"
            }
        };

        foreach (var data in routes)
        {
            GameObject cardGO = Instantiate(routeCardPrefab, contentParent);
            cardGO.GetComponent<RouteCard>().Setup(data);
        }
    }
}