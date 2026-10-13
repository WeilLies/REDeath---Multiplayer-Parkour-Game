using System.Collections.Generic;
using UnityEngine;

public class GameEntryPoint : MonoBehaviour
{
    [SerializeField] private List<GameObject> initServices = new List<GameObject>();
    private List<GameObject> services = new List<GameObject>();

    private void Start()
    {
        CreateAllInitService();
    }
    private void CreateAllInitService()
    {
        foreach (GameObject service in initServices)
        {
            Instantiate(service);
        }
    }
    private void DestroyAllInitService()
    {
        foreach (GameObject service in services)
        {
            Destroy(service);
        }
    }
}
