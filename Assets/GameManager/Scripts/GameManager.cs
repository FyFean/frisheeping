using UnityEngine;
using System.Collections.Generic;
using csDelaunay;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
  // game settings
  [Header("N of sheep")]
  public int nOfSheep = 100;
  [HideInInspector]
  public int sheepCount;

  // GUI
  [Header("GUI")]
  public Text countdownText;
  public Text scoreText;

  // timer
  private float gameTimer = 150.0f;

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
  public float d_R = 31.6f;
  public float d_S = 6.3f;

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
  private float neighboursUpdateInterval = 0*.5f;
  private float neighboursTimer;

  void Start()
  {
    // spawn
    SpawnSheep();

    // fences colliders
    fenceColliders = fence.GetComponentsInChildren<Collider>();

    // timers
    neighboursTimer = neighboursUpdateInterval;
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
    // remove spawn areas
    foreach (GameObject area in GameObject.FindGameObjectsWithTag("SpawnArea"))
      GameObject.Destroy(area);
  }

  public void Quit()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
  }

  void Update()
  {
    // pause menu
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      Quit();
    }

    // update
    UpdateNeighbours();

    // game timer
    gameTimer -= Time.deltaTime;

    if (gameTimer > 10.0f)
      countdownText.text = ((int)Mathf.Ceil(gameTimer)).ToString();
    else
    {
      gameTimer = Mathf.Max(gameTimer, .0f);
      countdownText.text = ((float)System.Math.Round(gameTimer, 2)).ToString();
    }

    scoreText.text = sheepCount.ToString();

    if (gameTimer == 0 || sheepCount <= 0)
    {
      // TODO save score - number of dogs, remaining time, remaining sheep
      Quit();
    }
  }

  private void UpdateNeighbours()
  {
    neighboursTimer -= Time.deltaTime;
    if (neighboursTimer < 0)
    {
      // find dogs
      dogs = new List<DogController>(FindObjectsOfType<DogController>());

      neighboursTimer = neighboursUpdateInterval;

      List<Vector2f> points = new List<Vector2f>();

      // prepare for Fortunes algorithm and clear neighbours
      foreach (SheepController sheep in sheepList)
      {
        if (!sheep.dead)
        {
          // perform updates by swap to prevent empty lists due to asynchronous execution
          List<DogController> dogNeighbours = new List<DogController>();

          points.Add(new Vector2f(sheep.transform.position.x, sheep.transform.position.z, sheep.id));

          // dogs
          foreach (DogController DC in dogs)
          {
            if ((sheep.transform.position - DC.transform.position).sqrMagnitude < sheep.dogRepulsion2)
              dogNeighbours.Add(DC);
          }

          // perform updates by swap to prevent empty lists due to asynchronous execution
          sheep.dogNeighbours = dogNeighbours;
        }
      }

      // topologic neighbours
      Rectf bounds = new Rectf(-50.0f, -60.0f, 100.0f, 100.0f);
      Voronoi voronoi = new Voronoi(points, bounds);

      foreach (Vector2f pt in points)
      {
        SheepController sheep = sheepList[pt.id];
        if (!sheep.dead)
        {
          // perform updates by swap to prevent empty lists due to asynchronous execution
          List<SheepController> metricNeighbours = new List<SheepController>();
          List<SheepController> topologicNeighbours = new List<SheepController>();

          foreach (Vector2f neighbourPt in voronoi.NeighborSitesForSite(pt))
          {
            SheepController neighbour = sheepList[neighbourPt.id];
            topologicNeighbours.Add(neighbour);

            // note that this may not include all true metric neighbours
            if ((sheep.transform.position - neighbour.transform.position).sqrMagnitude < sheep.r_o2)
              metricNeighbours.Add(neighbour);
          }

          // perform updates by swap to prevent empty lists due to asynchronous execution
          sheep.topologicNeighbours = topologicNeighbours;
          sheep.metricNeighbours = metricNeighbours;
        }
      }

#if false
      Debug.DrawLine(new Vector3(bounds.x, 0, bounds.y), new Vector3(bounds.x + bounds.width, 0, bounds.y));
      Debug.DrawLine(new Vector3(bounds.x + bounds.width, 0, bounds.y), new Vector3(bounds.x + bounds.width, 0, bounds.y + bounds.height));
      Debug.DrawLine(new Vector3(bounds.x + bounds.width, 0, bounds.y + bounds.height), new Vector3(bounds.x, 0, bounds.y + bounds.height));
      Debug.DrawLine(new Vector3(bounds.x, 0, bounds.y + bounds.height), new Vector3(bounds.x, 0, bounds.y));
      foreach (LineSegment ls in voronoi.VoronoiDiagram())
      {
        Debug.DrawLine(new Vector3(ls.p0.x, 0f, ls.p0.y), new Vector3(ls.p1.x, 0f, ls.p1.y), Color.black);
      }
#endif
    }
  }
}