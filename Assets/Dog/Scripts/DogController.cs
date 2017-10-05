using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class DogController : MonoBehaviour
{
  public bool Strombom = false;
  public bool Local = false;
  public float ro = 1.5f;// length equal to SheepController::r_o 
  public float rs = 3 * 1.5f;// length at which dog stops 3ro
  public float rw = 6 * 1.5f;// length at which dog starts walking
  public float rr = 9 * 1.5f;// length at which dog starts running
  public int ns = 20; // size of local subgroups
  public float blindAngle = 60f;

  // state
  [HideInInspector]
  public Enums.DogState dogState;
  public Enums.DogState previousDogState;

  // Game settings
  [HideInInspector]
  public int controls;

  // GameManager
  private GameManager GM;

  // Dogs animator controller
  public Animator anim;

  // Movement parameters
  private float maxSpeed = 10.0f;
  private float minSpeed = -3.0f;
  private float desiredV = .0f;
  private float v;
  private float maxSpeedChange = 1.0f/.016f;

  private float theta;
  private float desiredTheta = .0f;
  private float maxTurn = 4.5f/.016f; // in deg

  // animation bools
  private bool turningLeft = false;
  private bool turningRight = false;

  // Use this for initialization
  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // init speed
    desiredV = 5.0f;
    v = desiredV;

    // heading is current heading
    desiredTheta = Mathf.Atan2(transform.forward.z, transform.forward.y) * Mathf.Rad2Deg;
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;
  }

  // Update is called once per frame
  void Update()
  {
    if (Strombom)
      BehaviourLogic();
    else
      Controls();

    DogMovement();
  }

  void Controls()
  {
    if(Input.GetKey(KeyCode.W))
    {
      desiredV += maxSpeedChange * Time.deltaTime;
    }
    else if (Input.GetKey(KeyCode.S))
    {
      desiredV -= maxSpeedChange * Time.deltaTime;
    }
    else
    {
      desiredV = Mathf.MoveTowards(desiredV, 0, maxSpeedChange / 10f); 
    }

    if (Input.GetKey(KeyCode.A))
    {
      desiredTheta += maxTurn * Time.deltaTime;
    }
    else if (Input.GetKey(KeyCode.D))
    {
      desiredTheta -= maxTurn * Time.deltaTime;
    }
    else
    {
      desiredTheta = theta + Mathf.MoveTowardsAngle((desiredTheta - theta), 0, maxTurn  / 10f);
      // ensure angle remains in [-180,180)
      desiredTheta = (desiredTheta + 180f) % 360f - 180f;
    }
  }

  public class ByDistanceFrom : IComparer<SheepController>
  {
    public Vector3 position { get; set; }
    public ByDistanceFrom(Vector3 pos) { position = pos; }
    public int Compare(SheepController sc1, SheepController sc2)
    {
      float dsc1 = (sc1.transform.position - position).sqrMagnitude;
      float dsc2 = (sc2.transform.position - position).sqrMagnitude;

      if (dsc1 > dsc2) return 1;
      if (dsc1 < dsc2) return -1;
      return 0;
    }
  }

  private bool IsVisible(SheepController sc)
  {
    // experimental: test for visibility
    Vector3 toSc = sc.transform.position - transform.position;
#if false
    RaycastHit hit;
    Vector3 toCm = sc.GetComponent<Rigidbody>().worldCenterOfMass - GetComponent<Rigidbody>().worldCenterOfMass;
    if (Physics.Raycast(GetComponent<Rigidbody>().worldCenterOfMass + .5f*toCm.normalized, toCm.normalized, out hit, toCm.magnitude - 1f))
    {
//      Debug.DrawRay(GetComponent<Rigidbody>().worldCenterOfMass, toCm.normalized * hit.distance, Color.red);
    }
    else
    {
      Debug.DrawRay(GetComponent<Rigidbody>().worldCenterOfMass, toCm, Color.white);
    }
#endif
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }

  void BehaviourLogic()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;

    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    // get only live sheep
    List<SheepController> sheepList = new List<SheepController>(GM.sheepList).Where(sheep => !sheep.dead).ToList();
    if (Local)
    { // localized perception
      sheepList = sheepList.Where(sheep => IsVisible(sheep)).ToList();

#if true // experimental: exlude visually occludded sheep
      sheepList.Sort(new ByDistanceFrom(transform.position));
      List<int> hidden = new List<int>();
      for (int i = 0; i < sheepList.Count; i++)
      {
        Vector3 toSc = sheepList[i].transform.position - transform.position;
        float dcos = Mathf.Atan2(.5f*sheepList[i].transform.localScale.x, toSc.magnitude);
        float cos = Mathf.Acos(Vector3.Dot(transform.forward, toSc.normalized));
        for (int j = i+1; j < sheepList.Count; j++)
        {
          if (hidden.Exists(k => k == sheepList[j].id)) continue; // skip those already hidden

          Vector3 toSc2 = sheepList[j].transform.position - transform.position;
          float dcos2 = Mathf.Atan2(.5f*sheepList[j].transform.localScale.x, toSc2.magnitude);
          float cos2 = Mathf.Acos(Vector3.Dot(transform.forward, toSc2.normalized));

          float visible = Mathf.Max(0, Mathf.Min(cos - dcos, cos2 + dcos2) - (cos2 - dcos2));
          visible += Mathf.Max(0, (cos2 + dcos2) - Mathf.Max(cos2 - dcos2, cos + dcos));
          if (visible/dcos2 <= 1) hidden.Add(sheepList[j].id);
        }
      }
      for (int i = 0; i < sheepList.Count; i++)
      {
        if (!hidden.Exists(j => j == sheepList[i].id))
          Debug.DrawRay(transform.position, sheepList[i].transform.position - transform.position, Color.white);
      }

      sheepList = sheepList.Where(sheep => !hidden.Exists(id => id == sheep.id)).ToList();
#else
     //sheepList = sheepList.GetRange(0, Mathf.Min(ns, sheepList.Count));
#endif
    }

    // compute CM of sheep
    Vector3 CM = new Vector3();
    foreach (SheepController sheep in sheepList)
      CM += sheep.transform.position;
    if (sheepList.Count > 0)
      CM /= (float)sheepList.Count;

    // draw CM
    Vector3 X = new Vector3(1, 0, 0);
    Vector3 Z = new Vector3(0, 0, 1);
    Color color = new Color(0f, 0f, 0f, 1f);
    Debug.DrawRay(CM - X, 2*X, color);
    Debug.DrawRay(CM - Z, 2*Z, color);

    // find distance of sheep that is nearest to the dog & distance of sheep furthest from CM
    float md_ds = Mathf.Infinity;
    SheepController sheep_c = new SheepController();
    float Md_sC = 0;
    float nnd = 0; // mean nnd
    foreach (SheepController sheep in sheepList)
    {
      // distance from CM
      float d_sC = (CM - sheep.transform.position).magnitude;
      if (d_sC > Md_sC)
      {
        Md_sC = d_sC;
        sheep_c = sheep;
      }

      // distance from dog
      float d_ds = (sheep.transform.position - transform.position).magnitude;
      md_ds = Mathf.Min(md_ds, d_ds);

      float nn = Mathf.Infinity;
      foreach (SheepController sc in sheepList)
      {
        if (sc.id == sheep.id) continue;
        nn = Mathf.Min(nn, (sheep.transform.position - sc.transform.position).magnitude);
      }
      nnd += nn;
    }
    nnd /= sheepList.Count;
    ro = nnd;

    // if too close to any sheep stop and wait
    if (md_ds < rs)
    {
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }
    // if close to any sheep start walking
    else if (md_ds < rw)
    {
      dogState = Enums.DogState.walking;
      desiredV = maxSpeed * .5f;
    }
    else if (md_ds > rr)
    {
      // default run in current direction
      desiredV = maxSpeed * .75f;
      dogState = Enums.DogState.running;
    }

    // aproximate radius of a circle
    float f_N = ro*Mathf.Pow(sheepList.Count, 2f / 3f) / 2f;
    // draw aprox herd size
    int prec = 36;
    color = new Color(1f, 0f, 0f, 1f);
    for (int i = 0; i < prec;  i++)
    {
      float phi = 2f * Mathf.PI * i / prec;
      Vector3 r = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
      float phi1 = 2f * Mathf.PI * (i + 1) / prec;
      Vector3 r1 = new Vector3(Mathf.Cos(phi1), 0f, Mathf.Sin(phi1));

      Debug.DrawLine(CM + f_N*r, CM + f_N*r1, color);
    }

#if true
    foreach (SheepController sheep in sheepList)
    {
      prec = 36;
      color = new Color(1f, 0f, 0f, 1f);
      for (int i = 0; i < prec; i++)
      {
        float phi = 2f * Mathf.PI * i / prec;
        Vector3 r = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
        float phi1 = 2f * Mathf.PI * (i + 1) / prec;
        Vector3 r1 = new Vector3(Mathf.Cos(phi1), 0f, Mathf.Sin(phi1));

        Debug.DrawLine(sheep.transform.position + .5f * r, sheep.transform.position + .5f * r1, color);
      }
    }
#endif

    // if all agents in a single compact group, collect them
    if (Md_sC < f_N)
    {
      BarnController barn = FindObjectOfType<BarnController>();

      // compute position so that the GCM is on a line between the dog and the target
      Vector3 Pd = CM + (CM - barn.transform.position).normalized * Mathf.Min(ro * Mathf.Sqrt(sheepList.Count), Md_sC);
      desiredThetaVector = Pd - transform.position;
      if (desiredThetaVector.magnitude < rw)
        desiredV = maxSpeed * .5f;

      color = new Color(0f, 1f, 0f, 1f);
      Debug.DrawRay(Pd - X - Z, 2*X, color);
      Debug.DrawRay(Pd + X - Z, 2*Z, color);
      Debug.DrawRay(Pd + X + Z, -2*X, color);
      Debug.DrawRay(Pd - X + Z, -2*Z, color);
    }
    else
    {
      // compute position so that the sheep most distant from the GCM is on a line between the dog and the GCM
      Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
      // move in an arc around the herd??
      desiredThetaVector = Pc - transform.position;

      color = new Color(1f, .5f, 0f, 1f);
      Debug.DrawRay(Pc - X - Z, 2*X, color);
      Debug.DrawRay(Pc + X - Z, 2*Z, color);
      Debug.DrawRay(Pc + X + Z, -2*X, color);
      Debug.DrawRay(Pc - X + Z, -2*Z, color);
    }

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
    /* end of behaviour logic */
  }

  void DogMovement()
  {
    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, maxTurn * Time.deltaTime);
    // ensure angle remains in [-180,180)
    theta = (theta + 180f) % 360f - 180f;
    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, maxSpeedChange * Time.deltaTime);
    // ensure speed remains in [minSpeed, maxSpeed]
    v = Mathf.Clamp(v, minSpeed, maxSpeed);

    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;
    // update position
    Vector3 newPosition = transform.position + (Time.deltaTime * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    if (v == .0f)
    {
      turningLeft = false;
      turningRight = false;
    }
    else
    {
      turningLeft = theta > 0;
      turningRight = theta < 0;
    }

    // Animation Controller
    anim.SetBool("IsRunning", v > 8);
    anim.SetBool("Reverse", v < 0);
    anim.SetBool("IsWalking", (v > 0 && v <= 8) ||
                               turningLeft ||
                               turningRight);
  }
}