using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    // Singleton Instance
    public static MapManager Instance { get; private set; }

    [SerializeField] private PlayerMoveController3 playerCtrl;

    [SerializeField]
    private GameObject defalutMapPrefab;

    [SerializeField] float changeMapTime = 0.5f;
    [SerializeField] UIFadeInOut panel;
    [SerializeField] bool debugMode = false;

    private MapContainer CurrentMap { get; set; }
    private Dictionary<string, MapContainer> mapStore = new();

    // Singleton Pattern
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (CurrentMap == null)
        {
            FirstMap(defalutMapPrefab);
        }
    }

    public void FirstMap(GameObject newMap)
    {
        StartCoroutine(ChangeMap(newMap, Vector2Int.zero));
    }

    public IEnumerator ChangeMap(GameObject newMap, Vector2Int pos, bool destroy = false)
    {
        bool hasPrevMap = mapStore.Count > 0;
        if (hasPrevMap)
        {
            StartCoroutine(panel.FadeInOut(changeMapTime, 0.3f, 0.5f));
            StartCoroutine(playerCtrl.Teleport(pos, changeMapTime));
            playerCtrl.SetFloor(1);
            yield return new WaitForSeconds(changeMapTime);
            if (destroy) FreeMap(CurrentMap);
        }

        CurrentMap = LoadMap(newMap);
        TriggerExecutor.Instance.ChangeMap(CurrentMap.GridLayout, CurrentMap.TriggerTilemap);
        TriggerExecutor.Instance.ChangeC2E(CurrentMap.GetC2E());
        CurrentMap.debugMode = debugMode;
    }

    private MapContainer LoadMap(GameObject mapPrefab)
    {
        MapContainer loadMap;
        // 메모리에 맵에 올라와 있을 때
        if (mapStore.ContainsKey(mapPrefab.name))
        {
            loadMap = mapStore[mapPrefab.name];
        } 
        else
        {
            loadMap = Instantiate(mapPrefab).GetComponent<MapContainer>();
            loadMap.gameObject.name = mapPrefab.name;   
            mapStore.TryAdd(mapPrefab.name, loadMap);
        }

        DeactiveAllMap(loadMap);
        return loadMap;
    }

    private void FreeMap(MapContainer mapCont)
    {
        mapStore.Remove(mapCont.gameObject.name);
        Destroy(mapCont.gameObject);
    }

    private void DeactiveAllMap(MapContainer only)
    {
        foreach (MapContainer map in mapStore.Values)
        {
            if (map == only)
            {
                map.gameObject.SetActive(true);
                continue;
            }
            map.gameObject.SetActive(false);
        }
    }

    public void ChangePlayerFloor(int floor)
    {
        playerCtrl.SetFloor(floor);
    }

    public GridLayout GetMapGrid()
    {
        return CurrentMap.GridLayout;
    }

    public GameObject GetMapTriggerMap()
    {
        return CurrentMap.TriggerTilemap;
    }

    public Transform GetTriggerTransform()
    {
        return CurrentMap.transform;
    }

    public bool IsCollision(Vector3 position) {
        return false;
    }

    // Util
    public Vector3Int World2Grid(Vector3 worldPosition) {
        return new Vector3Int(0, 0, 0);
    }

    public Vector3 Grid2World(Vector3Int gridPosition) {
        return new Vector3(0, 0, 0);
    }
}
