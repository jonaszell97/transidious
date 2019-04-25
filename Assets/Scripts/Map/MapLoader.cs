using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public Map map;
    public OSMImportHelper.Area area;

    // Use this for initialization
    void Start()
    {
        map.LoadFromFile(area.ToString());
    }

    // Update is called once per frame
    void Update()
    {

    }
}
