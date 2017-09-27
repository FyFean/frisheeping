using UnityEngine;
using System.Collections.Generic;
using csDelaunay;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
  // game settings
  [Header("Game Settings")]
  public int nOfSheep;
  [HideInInspector]
  public int sheepCount;

  // timer
  private float gameDuration = 150.0f;
  [HideInInspector]
  public float gameDurationTimer;

  // game settings
  // hardcoded spawn boundaries
  private float minSpawnX = -50.0f;
  private float maxSpawnX = 50.0f;
  private float maxSpawnZ = 30.0f;
  private float minSpawnZ = -55.0f;

  // sheep prefab
  [Header("Sheep")]
  public GameObject sheepPrefab;
  // random size
  private float minSheepSize = .7f;

  // list of sheep
  [HideInInspector]
  public List<SheepController> sheepList = new List<SheepController>();

  // list of dogs
  private List<DogController> dogs = new List<DogController>();

  // fences
  [Header("Fence")]
  public GameObject fence;
  [HideInInspector]
  public Collider[] fenceColliders;

  // skybox
  [Header("Skybox")]
  public Material[] skyboxes;

  // update frequency
  private float neighboursUpdateInterval = .5f;
  private float neighboursTimer;


  void Start()
  {
    // spawn
    SpawnSheep();

    // find dogs
    dogs = new List<DogController>(FindObjectsOfType<DogController>());

    // fences colliders
    fenceColliders = fence.GetComponentsInChildren<Collider>();

    // timers
    neighboursTimer = neighboursUpdateInterval;
    gameDurationTimer = gameDuration;
  }

  void SpawnSheep()
  {
    // number of sheep
    sheepCount = nOfSheep;

    // cleanup
    int i = 0;
    sheepList.Clear();
    GameObject[] sheep = GameObject.FindGameObjectsWithTag("Sheep");
    for (i = 0; i < sheep.Length; i++)
      Destroy(sheep[i]);

    // spawn
    Vector3 position;
    SheepController newSheep;

    i = 0;
    while (i < sheepCount)
    {
      position = new Vector3(Random.Range(minSpawnX, maxSpawnX), .0f, Random.Range(minSpawnZ, maxSpawnZ));

      if (Physics.CheckSphere(position, 1.0f, 1 << 8))
      {
        float randomFloat = Random.Range(minSheepSize, 1.0f);
        newSheep = ((GameObject)Instantiate(sheepPrefab, position, Quaternion.identity)).GetComponent<SheepController>();
        newSheep.id = i;
        newSheep.transform.localScale = new Vector3(randomFloat, randomFloat, randomFloat);
        sheepList.Add(newSheep);
        i++;
      }
    }
  }

  public void Quit()
  {
    Application.Quit();
  }

  void Update()
  {
    // pause menu
    if (Input.GetKeyDown(KeyCode.Escape))
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    // update
    UpdateNeighbours();

    // game timer
    gameDurationTimer -= Time.deltaTime;

    if (gameDurationTimer < 0 || sheepCount <= 0)
    {
      // end game
      // TODO save score
    }
  }

  private void UpdateNeighbours()
  {
    neighboursTimer -= Time.deltaTime;
    if (neighboursTimer < 0)
    {
      neighboursTimer = neighboursUpdateInterval;

      List<Vector2f> points = new List<Vector2f>();

      // prepare for Fortunes algorithm and clear neighbours
      foreach (SheepController sheep in sheepList)
      {
        if (!sheep.dead)
        {
          sheep.metricNeighbours.Clear();
          sheep.topologicNeighbours.Clear();
          sheep.dogNeighbours.Clear();

          points.Add(new Vector2f(sheep.transform.position.x, sheep.transform.position.z, sheep.id));

          // dogs
          foreach (DogController DC in dogs)
          {
            if ((sheep.transform.position - DC.transform.position).sqrMagnitude < sheep.dogRepulsion2)
              sheep.dogNeighbours.Add(DC);
          }
        }
      }

      // topologic neighbours
      Rectf bounds = new Rectf(-40.0f, -25.0f, 80.0f, 50.0f);
      Voronoi voronoi = new Voronoi(points, bounds);

      foreach (Vector2f pt in points)
      {
        SheepController sheep = sheepList[pt.id];
        if (!sheep.dead)
        {
          foreach (Vector2f neighbourPt in voronoi.NeighborSitesForSite(pt))
          {
            SheepController neighbour = sheepList[neighbourPt.id];
            sheep.topologicNeighbours.Add(neighbour);


            if ((sheep.transform.position - neighbour.transform.position).sqrMagnitude < sheep.r_o2)
              sheep.metricNeighbours.Add(neighbour);

          }
        }
      }
    }
  }
}