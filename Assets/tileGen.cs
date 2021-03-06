﻿using UnityEngine;
using System.Collections;
using System.Linq;

public class tileGen : MonoBehaviour
{
    Vector2[,] localTerrain = new Vector2[3, 3];
    GameObject[,] tiles = new GameObject[3, 3];
    float currX;
    float currZ;
    float oldX;
    float oldZ;
    public GameObject tile;

    public Vector2 Center;
    private Vector2 Position;
    private int Zoom = 16;
    string status = "start";
    // Use this for initialization

    IEnumerator Start()
    {
        SimplePool.Preload(tile, 15);
        currX = oldX = Mathf.Floor(transform.position.x);
        currZ = oldZ = Mathf.Floor(transform.position.z);
        // First, check if user has location service enabled
        // No location then fallback coordinates
        if (!Input.location.isEnabledByUser)
        {
            Center = calcTile(42.434605f, -83.984956f);
            Debug.Log(Center);
            Position = posInTile(42.434605f, -83.984956f);
            Debug.Log(Position);
            Vector3 pos = new Vector3((Position.x - 0.5f) * 611, 0, (0.5f - Position.y) * 611);
            transform.position = pos;
            Debug.Log(pos);
            status = "no location service";
            updateBoard();
            InvokeRepeating("updateLoc", 0.5f, 0.5f);
            yield break;
        }


        // Start service before querying location
        Input.location.Start(1,0.1f);
        status = "rev up";

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            status = "timed out";
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            status = "Unable to determine device location";
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            Center = calcTile(Input.location.lastData.latitude, Input.location.lastData.longitude);
            Debug.Log(Center);
            Position = posInTile(Input.location.lastData.latitude, Input.location.lastData.longitude);
            Debug.Log(Position);
            status = "Creating tile " + Center.x + ", " + Center.y;
            status = "Pos tile " + Position.x + ", " + Position.y;
            Vector3 pos = new Vector3((Position.x - 0.5f) * 611, 0, (0.5f - Position.y) * 611);
            transform.position = pos;
            updateBoard();
            InvokeRepeating("updateLoc", 0.5f, 0.5f);
        }
    }

    void updateLoc()
    {
        //updates location
        status = "repeating";
        Center = calcTile(Input.location.lastData.latitude, Input.location.lastData.longitude);
        Debug.Log(Center);
        Position = posInTile(Input.location.lastData.latitude, Input.location.lastData.longitude);
        Debug.Log(Position);
        Vector3 pos = new Vector3((Position.x - 0.5f) * 611, 0, (0.5f - Position.y) * 611);
        transform.position = pos;
    }

    // checks if movement is greate than a single tile space, if so update the board
    void Update()
    {
        currX = Mathf.Floor(transform.position.x) - Mathf.Floor(transform.position.x) % 611;
        currZ = Mathf.Floor(transform.position.z) - Mathf.Floor(transform.position.z) % 611;
        if (Mathf.Abs(currX - oldX) > 600 || Mathf.Abs(currZ - oldZ) > 600)
        {
            Debug.Log("UPDATE BOARD");
            updateBoard();
            oldX = currX;
            oldZ = currZ;
        }
    }

    //checks if theres a tile in that location, if not then put one down
    //TODO: check/fix moving several tiles over
    void updateBoard()
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                localTerrain[i + 1, j + 1] = new Vector2(Center.x + i, Center.y + j);
                if (!Physics.CheckSphere(new Vector3(currX + i * 306, 0, currZ + j * 611), 0.4f))
                {
                    tiles[i + 1, j + 1] = SimplePool.Spawn(tile, new Vector3(currX + i * 306, 0f, currZ + j * 611), Quaternion.identity);
                    StartCoroutine(tiles[i + 1, j + 1].GetComponent<Assets.Tile>().CreateTile(new Vector2(Center.x + i, Center.y - j), new Vector3(currX + i * 306, 0f, currZ + j * 611), 16));
                }
                else {
                    Collider[] temp = Physics.OverlapSphere(new Vector3(currX + i * 306, 0f, currZ + j * 611), 0.4f);
                    tiles[i + 1, j + 1] = temp[0].gameObject;
                }
            }
        }
        cleanup(0);
    }

    //cleanup tiles outside a certain range
    void cleanup(float sec)
    {
        Collider[] include = Physics.OverlapSphere(transform.position, 1800f);
        Collider[] exclude = Physics.OverlapSphere(transform.position, 10000f);
        var outside = exclude.Except(include);
        foreach (Collider g in outside)
        {
            if (g.tag == "Tile")
            {
                SimplePool.Despawn(g.gameObject);
            }
        }
    }

    Vector2 calcTile(float lat, float lng)
    {
        //pseudo
        //n = 2 ^ zoom
        //xtile = n * ((lon_deg + 180) / 360)
        //ytile = n * (1 - (log(tan(lat_rad) + sec(lat_rad)) / π)) / 2

        float n = Mathf.Pow(2, Zoom);
        float xtile = n * ((lng + 180) / 360);
        float ytile = n * (1 - (Mathf.Log(Mathf.Tan(Mathf.Deg2Rad * lat) + (1f / Mathf.Cos(Mathf.Deg2Rad * lat))) / Mathf.PI)) / 2f;
        return new Vector2((int)xtile, (int)ytile);
    }

    Vector2 posInTile(float lat, float lng)
    {
        float n = Mathf.Pow(2, Zoom);
        float xtile = n * ((lng + 180) / 360);
        float ytile = n * (1 - (Mathf.Log(Mathf.Tan(Mathf.Deg2Rad * lat) + (1f / Mathf.Cos(Mathf.Deg2Rad * lat))) / Mathf.PI)) / 2f;
        return new Vector2(xtile - (int)xtile, ytile - (int)ytile);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 30), Input.location.lastData.latitude.ToString());
        GUI.Label(new Rect(10, 50, 500, 30), Input.location.lastData.longitude.ToString());
        GUI.Label(new Rect(10, 100, 500, 30), status);
        //GUI.Label(new Rect(10, 10, 500, 30), Center.x.ToString());
        //GUI.Label(new Rect(10, 50, 500, 30), Center.y.ToString());
    }
}
