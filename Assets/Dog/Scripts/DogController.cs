using UnityEngine;

public class DogController : MonoBehaviour
{
  public bool Strombom = false;
  public float ro = 1.5f;// length equal to SheepController::r_o 
  public float rs = 3 * 1.5f;// length at which dog stops 3ro
  public float rw = 6 * 1.5f;// length at which dog starts walking
  public float rr = 9 * 1.5f;// length at which dog starts running

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
  private float maxTurn = .03f/.016f;

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
    desiredTheta = Mathf.Atan2(transform.forward.z, transform.forward.y);
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;
  }

  // Update is called once per frame
  void FixedUpdate()
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
      desiredTheta = theta + Mathf.MoveTowardsAngle(Mathf.Rad2Deg * (desiredTheta - theta), 0, Mathf.Rad2Deg * maxTurn  / 10f) * Mathf.Deg2Rad;
      // ensure angle remains in [-Pi,Pi)
      desiredTheta = (desiredTheta + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    }
  }

  void BehaviourLogic()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;
    
    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    // minimum distance to all sheep
    // compute GCM of sheep
    int sheepCount = 0;
    Vector3 GCM = new Vector3();
    foreach (SheepController sheep in GM.sheepList)
    {
      // exclude sheep already in barn
      if (sheep.dead) continue;

      GCM += sheep.transform.position;
      sheepCount++;
    }
    if (sheepCount > 0)
      GCM /= (float)sheepCount;

    Vector3 X = new Vector3(1, 0, 0);
    Vector3 Z = new Vector3(0, 0, 1);
    Color color = new Color(0f, 0f, 0f, 1f);
    Debug.DrawRay(GCM - X, 2*X, color);
    Debug.DrawRay(GCM - Z, 2*Z, color);

    // find distance of sheep that is nearest to the dog & distance of sheep furthest from GCM
    float md_ds = Mathf.Infinity;
    SheepController sheep_c = new SheepController();
    float Md_sG = 0;
    foreach (SheepController sheep in GM.sheepList)
    {
      // exclude sheep already in barn
      if (sheep.dead) continue;

      // distance from GCM
      float d_sG = (GCM - sheep.transform.position).magnitude;
      if (d_sG > Md_sG)
      {
        Md_sG = d_sG;
        sheep_c = sheep;
      }

      // distance from dog
      float d_ds = (sheep.transform.position - transform.position).magnitude;
      md_ds = Mathf.Min(md_ds, d_ds);
    }

    // if close to any sheep start walking
    if (md_ds < rw)
    {
      dogState = Enums.DogState.idle;
      desiredV = maxSpeed * .5f;
    }
    // if too close to any sheep stop and wait
    else if (md_ds < rs)
    {
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }
    else if (md_ds > rr)
    {
      // default run in current direction
      desiredV = maxSpeed * .75f;
      dogState = Enums.DogState.running;
    }

    float f_N = ro*Mathf.Pow(sheepCount, 2f / 3f);
    int prec = 10;
    color = new Color(1f, 0f, 0f, 1f);
    for (int i = 0; i < prec;  i++)
    {
      float phi = 2f * Mathf.PI * i / prec;
      Vector3 r = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
      float phi1 = 2f * Mathf.PI * (i + 1) / prec;
      Vector3 r1 = new Vector3(Mathf.Cos(phi1), 0f, Mathf.Sin(phi1));

      Debug.DrawLine(GCM + f_N*r, GCM + f_N*r1, color);
    }

    // if all agents in a single compact group, collect them
    if (Md_sG < ro * Mathf.Pow(sheepCount, 2f / 3f))
    {
      BarnController barn = FindObjectOfType<BarnController>();

      // compute position so that the GCM is on a line between the dog and the target
      Vector3 Pd = GCM + (GCM - barn.transform.position).normalized * Mathf.Min(ro * Mathf.Sqrt(sheepCount), Md_sG);
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
      Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - GCM).normalized * ro;
      // move in an arc around the herd??
      desiredThetaVector = Pc - transform.position;

      color = new Color(1f, .5f, 0f, 1f);
      Debug.DrawRay(Pc - X - Z, 2*X, color);
      Debug.DrawRay(Pc + X - Z, 2*Z, color);
      Debug.DrawRay(Pc + X + Z, -2*X, color);
      Debug.DrawRay(Pc - X + Z, -2*Z, color);
    }

    // extract desired heading
    desiredTheta = Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps;
    /* end of behaviour logic */
  }

  void DogMovement()
  {
    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(Mathf.Rad2Deg * theta, Mathf.Rad2Deg * desiredTheta, Mathf.Rad2Deg * maxTurn * Time.deltaTime) * Mathf.Deg2Rad;
    // ensure angle remains in [-Pi,Pi)
    theta = (theta + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, maxSpeedChange * Time.deltaTime);
    // ensure speed remains in [minSpeed, maxSpeed]
    v = Mathf.Clamp(v, minSpeed, maxSpeed);

    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;
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