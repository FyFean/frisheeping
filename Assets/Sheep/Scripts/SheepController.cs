#define DEBUG_ON

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

public class SheepController : MonoBehaviour
{
  // id
//  [HideInInspector]
  public int id;

  // state
  [HideInInspector]
  public Enums.SheepState sheepState;
  public Enums.SheepState previousSheepState;

  // Sheeps Animator Controller
  public Animator anim;

  // Fur parts
  public Renderer[] sheepCottonParts;

  // GameManager
  private GameManager GM;

  // heading and postion
  private float desiredTheta = .0f;
  private float theta;
  private float maxTurn = 4.5f/.02f; // in deg

  // barn interaction
  /*
  private float barnWeight = 3.0f;
  private float barnRepulsion = 5.0f;
  [HideInInspector]
  public float barnRepulsion2;
  */
  // fence interacion
  private float fenceWeight = 2.0f;
  private float fenceRepulsion = 5.0f;
  [HideInInspector]
  public float fenceRepulsion2;

  // dog interaction
  private float dogWeight = 3.0f;
  private float dogRepulsion = 20.0f;
  private float strongDogRepulsion = 12.0f;

  [HideInInspector]
  public float dogRepulsion2;
  public float nearestDogDistance;

  // neighbour interaction
  private float r_o = 1.0f; // original 1.0f
  private float r_e = 1.0f; // original 1.0f
  public int ns = 8; // experimental: interact with maximally ns neighbours (cognitive limit)
  private float blindAngle = 0f;
  [HideInInspector]
  public float r_o2;

  // speed
  private float maxSpeedChange = .15f/.02f;
  private float desiredV = .0f;
  private float v;
  private float v_1 = 1.5f; // original 0.15f
  private float v_2 = 7.5f; // original 1.5f

  // noise
  private float eta = 0.13f; // original 0.13f

  // beta - cohesion factor
  private float beta = 3.0f;//3.0f; // original 0.8f

  // alpha, delta, transition parameters
  private float alpha = 15.0f;
  private float delta = 4.0f;
  private float tau_iw = 35f;//1.0f; // original 35
  private float tau_wi = 8f;//15.0f; // original 8
  private float tau_iwr;
  private float tau_ri;
  private float d_R = 31.6f;//15.0f; // original 31.6f
  private float d_S = 6.3f;//.66f; // original 6.3f
  private int n_idle = 0, n_walking = 0, m_toidle = 0, m_idle = 0, m_running = 0;
  float l_i = .0f;

  // neighbour list
  [HideInInspector]
  public List<SheepController> metricNeighbours = new List<SheepController>();
  [HideInInspector]
  public List<SheepController> topologicNeighbours = new List<SheepController>();
  [HideInInspector]
  public List<DogController> dogNeighbours = new List<DogController>();

  // debug parameters
  //private MeshRenderer meshRenderer;

  // update timers
  private float stateUpdateInterval = 0*.5f;//2*.5f;
  private float stateTimer;
  private float drivesUpdateInterval = 0*.02f;//1*.2f;
  private float drivesTimer;

  // dead flag
  [HideInInspector]
  public bool dead = false;

  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // interaction squared
    //barnRepulsion2 = barnRepulsion * barnRepulsion;
    fenceRepulsion2 = fenceRepulsion * fenceRepulsion;
    dogRepulsion2 = dogRepulsion * dogRepulsion;
    r_o2 = r_o * r_o;

    // get mesh renderer for debug coloring purposes
    //meshRenderer = GetComponentInChildren<MeshRenderer>();
    //meshRenderer.material.color = new Color(1.0f, 1.0f, .0f);

    // random state
    sheepState = (Enums.SheepState)Random.Range(0, 3);
    previousSheepState = sheepState;

    // speed
    SetSpeed();
    v = desiredV;

    // transition parameters
    tau_iwr = GM.sheepCount;
    tau_ri = GM.sheepCount;

    // random heading
    desiredTheta = Random.Range(-180f, 180f);
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

    // timer
    stateTimer = Random.Range(.0f, stateUpdateInterval);
    drivesTimer = Random.Range(.0f, drivesUpdateInterval);

    Color cottonColor = Color.white;
    // Asign random collor to sheeps fur

    // ----- Funky Easter Egg -----
    /*if (Random.value < 0.001)
    {
        cottonColor = new Color(Random.value, Random.value, Random.value, 1.0f);
    }
    else*/
    if (Random.value < .05f)
    {
      float blackShade = Random.Range(0.2f, 0.3f);
      cottonColor = new Color(blackShade, blackShade, blackShade, 1.0f);
    }
    else
    {
      float grayShade = Random.Range(0.7f, .9f);
      cottonColor = new Color(grayShade, grayShade, grayShade, 1.0f);
    }

    foreach (Renderer fur in sheepCottonParts)
    {
      if (fur.materials.Length < 2) fur.material.color = cottonColor;
      else fur.materials[1].color = cottonColor;
    }
  }

  void SetSpeed()
  {
#if DEBUG_ON
    Color cottonColor = Color.white;
#endif

    // debug coloring and speed
    switch (sheepState)
    {
      case Enums.SheepState.idle:
        desiredV = .0f;
#if DEBUG_ON
        cottonColor = new Color(.2f, .2f, .2f, 1.0f);
#endif
        //meshRenderer.material.color = new Color(1.0f, 1.0f, .0f);
        break;
      case Enums.SheepState.walking:
        desiredV = v_1;
#if DEBUG_ON
        cottonColor = new Color(1.0f, .5f, .0f, 1.0f);
#endif
        //meshRenderer.material.color = new Color(.0f, .0f, 1.0f);
        break;
      case Enums.SheepState.running:
        desiredV = v_2;
#if DEBUG_ON
        cottonColor = new Color(.0f, 1.0f, .0f, 1.0f);
#endif
        //meshRenderer.material.color = new Color(1.0f, .0f, .0f);
        break;
    }

#if DEBUG_ON
    foreach (Renderer fur in sheepCottonParts)
    {
      if (fur.materials.Length < 2) fur.material.color = cottonColor;
      else fur.materials[1].color = cottonColor;
    }
#endif
  }

  void Update()
  {
    /* behavour logic */
    drivesTimer -= Time.deltaTime;
    stateTimer -= Time.deltaTime;

    if (GM.Strombom)
    {
      // drives update
      if (drivesTimer < 0)
      {
        StrombomUpdate();

        drivesTimer = drivesUpdateInterval;
      }
    }
    else
    {
      // neighbours are needed both for state and drives updates
      if (stateTimer < 0 || drivesTimer < 0)
        NeighboursUpdate();

      // state update
      if (stateTimer < 0)
      {
        UpdateState();
        stateTimer = stateUpdateInterval;
      }

      // drives update
      if (drivesTimer < 0)
      {
        // only change speed and heading if not idle
        if (sheepState == Enums.SheepState.walking || sheepState == Enums.SheepState.running)
          DrivesUpdate();

        drivesTimer = drivesUpdateInterval;
      }
    }
    /* end of behaviour logic */

    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, maxTurn * Time.deltaTime);
    // ensure angle remains in [-180,180)
    theta = (theta + 180f) % 360f - 180f;
    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, maxSpeedChange * Time.deltaTime);

    // update position
    Vector3 newPosition = transform.position + (Time.deltaTime * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    // Sheep state animation
    anim.SetBool("IsIdle", sheepState == Enums.SheepState.idle);
    anim.SetBool("IsRunning", sheepState == Enums.SheepState.running);

#if true
    if (UnityEditor.Selection.activeGameObject.GetComponent<SheepController>().id == id)
    {
      int prec = 36;
      Color color = new Color(1f, 0f, 0f, 1f);
      for (int i = 0; i < prec; i++)
      {
        float phi = 2f * Mathf.PI * i / prec;
        Vector3 r = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
        float phi1 = 2f * Mathf.PI * (i + 1) / prec;
        Vector3 r1 = new Vector3(Mathf.Cos(phi1), 0f, Mathf.Sin(phi1));

        Debug.DrawLine(transform.position + r_o * r, transform.position + r_o * r1, color);
        Debug.DrawLine(transform.position + l_i * r, transform.position + l_i * r1, new Color(1f, 0f, 0f, 1f));
        Debug.DrawLine(transform.position + d_R * r, transform.position + d_R * r1, new Color(0f, 1f, 0f, 1f));
        Debug.DrawLine(transform.position + d_S * r, transform.position + d_S * r1, new Color(0f, 0f, 0f, 1f));

        foreach (SheepController s in metricNeighbours)
          Debug.DrawLine(s.transform.position + .5f * r, s.transform.position + .5f * r1, new Color(1f, 1f, 1f, 1f));
        foreach (SheepController s in topologicNeighbours)
          Debug.DrawLine(s.transform.position + .5f * r, s.transform.position + .5f * r1, new Color(0f, 1f, 1f, 1f));
      }
    }
#endif
  }

  public bool IsVisible(SheepController sc)
  {
#if true // experimental: test occlusion
    Vector3 toCm = sc.GetComponent<Rigidbody>().worldCenterOfMass - GetComponent<Rigidbody>().worldCenterOfMass;
    bool hit = Physics.Raycast(GetComponent<Rigidbody>().worldCenterOfMass + .5f * toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
    Vector3 toSc = sc.transform.position - transform.position;
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }

  void NeighboursUpdate()
  {
    n_idle = 0;
    n_walking = 0;
    m_toidle = 0;
    m_idle = 0;
    m_running = 0;

    l_i = .0f;

    foreach (SheepController neighbour in metricNeighbours)
    {
      // state counter
      switch (neighbour.sheepState)
      {
        case Enums.SheepState.idle:
          n_idle++;
          break;
        case Enums.SheepState.walking:
          n_walking++;
          break;
      }
    }

    // count because of destroyers
    int topologicNeighbourCount = 0;

    foreach (SheepController neighbour in topologicNeighbours)
    {
      topologicNeighbourCount++;
      // state count
      switch (neighbour.sheepState)
      {
        case Enums.SheepState.idle:
          if (neighbour.previousSheepState == Enums.SheepState.running)
            m_toidle++;
          m_idle++;
          break;
        case Enums.SheepState.running:
          m_running++;
          break;
      }

      // mean distance to topologic calculate
      l_i += (transform.position - neighbour.transform.position).magnitude;
    }
    // divide with number of topologic
    if (topologicNeighbourCount > 0)
      l_i /= topologicNeighbourCount;
    else
      l_i = .0f;
  }

  void UpdateState()
  {
    float dt = stateUpdateInterval;
    if (stateUpdateInterval <= 0f)
      dt = Time.deltaTime;

    previousSheepState = sheepState;

    float d_i = dogRepulsion2; // min distance to dogNeighbours
    // if dog detected switch state differently
    // if (dogNeighbours.Count > 0)
    { // this could be coded like piwr & pri the l_i/d_R, d_S/l_i parts, but l_i being the mean distance to the dog(s)
      // dog repulsion
      foreach (DogController DC in dogNeighbours)
      {
        float dist = (DC.transform.position - transform.position).magnitude;

#if false // experiment with dogRepulsion not forcing state change
        // override state and speed
        // running when dog is close, walking when dog is medium distance away
        if (dist < strongDogRepulsion)
          sheepState = Enums.SheepState.running;
        else if (sheepState != Enums.SheepState.running)
          sheepState = Enums.SheepState.walking;
#endif
        d_i = Mathf.Min(d_i, dist); 
      }
    }
    // else
    {
      // set transition parameters in case sheep number changes
      tau_iwr = GM.sheepCount;
      tau_ri = GM.sheepCount;

      // probabilities
      float p_iw = (1 + alpha * n_walking) / tau_iw;
      p_iw = 1 - Mathf.Exp(-p_iw * dt);
      float p_wi = (1 + alpha * n_idle) / tau_wi;
      p_wi = 1 - Mathf.Exp(-p_wi * dt);
      float p_iwr = .0f;
      float p_ri = 1.0f; // since l_i is in the denominator of the eq for p_ri

      if (l_i > .0f)
      {
#if true // experiment with dogRepulsion not forcing state change
        if (d_i < strongDogRepulsion) // feel unsafe much sooner
          d_R = GM.d_R * .1f;
        else if (d_i < dogRepulsion) // feel unsafe sooner
          d_R = GM.d_R * .75f;
        else
          d_R = GM.d_R;
#endif
        p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + alpha * m_running), delta);
        p_iwr = 1 - Mathf.Exp(-p_iwr * dt);

#if true // experiment with dogRepulsion not forcing state change
        if (d_i < strongDogRepulsion) // feel unsafe much sooner
          d_S = GM.d_S * .1f;
        else if (d_i < dogRepulsion) // feel unsafe sooner
          d_S = GM.d_S * .75f;
        else
          d_S = GM.d_S;
#endif
        p_ri = (1 / tau_ri) * Mathf.Pow((d_S / l_i) * (1 + alpha * m_toidle), delta);
        p_ri = 1 - Mathf.Exp(-p_ri * dt);
      }

      if (d_i < r_o) //GM.d_S)
      { // if dog enters safe range (range when stop) start running
        p_ri = 0f;
        p_iwr = 1f;
      }

      // test states
      float random = .0f;

      // first test the transition between idle and walking and viceversa
      if (sheepState == Enums.SheepState.idle)
      {
        random = Random.Range(.0f, 1.0f);
        if (random < p_iw)
          sheepState = Enums.SheepState.walking;
      }
      else // added to reflect SheepOptimization code
      if (sheepState == Enums.SheepState.walking)
      {
        random = Random.Range(.0f, 1.0f);
        if (random < p_wi)
          sheepState = Enums.SheepState.idle;
      }

      // second test the transition to running
      // which has the same rate regardless if you start from walking or idle
      if (sheepState == Enums.SheepState.idle || sheepState == Enums.SheepState.walking)
      {
        random = Random.Range(.0f, 1.0f);
        if (random < p_iwr)
          sheepState = Enums.SheepState.running;
      }
      else // added to reflect SheepOptimization code
      // while testing the transition to running also test the transition from running to standing
      if (sheepState == Enums.SheepState.running)
      {
        random = Random.Range(.0f, 1.0f);
        if (random < p_ri)
          sheepState = Enums.SheepState.idle;
      }
    }

    SetSpeed();
  }

  void DrivesUpdate()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;

    // declarations
    Vector3 e_ij;
    float f_ij, d_ij;

    // dog repulsion
    foreach (DogController DC in dogNeighbours)
    {
      e_ij = DC.transform.position - transform.position;
      d_ij = e_ij.magnitude;

#if false // sheep fear controlled in UpdateState
      // override state and speed
      // running when dog is close, walking when dog is medium distance away
      if (e_ij.magnitude < strongDogRepulsion)
      {
        sheepState = Enums.SheepState.running;
        v = v_2;
      }
      else
      {
        if (sheepState != Enums.SheepState.running)
        {
          sheepState = Enums.SheepState.walking;
          v = v_1;
        }
      }
#endif
      // TODO: intensify repulsion if dog is going streight for me
      f_ij = strongDogRepulsion / d_ij;
      desiredThetaVector += dogWeight * f_ij * -e_ij.normalized;
    }

    Vector3 closestPoint;
    // barn repulsion
    /*
    // get dist
    closestPoint = GM.barnCollider.ClosestPointOnBounds(transform.position);
    if ((transform.position - closestPoint).sqrMagnitude < barnRepulsion2)
    {
      e_ij = closestPoint - transform.position;

      // weak repulsion
      dot = Vector3.Dot(e_ij.normalized, transform.forward);
      s_f = transform.forward - (dot * e_ij);
      desiredTheta += s_f * barnWeight;    
    }
    */

    // fences repulsion
    foreach (Collider fenceCollider in GM.fenceColliders)
    {
      // get dist
      closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
      if ((transform.position - closestPoint).sqrMagnitude < fenceRepulsion2)
      {
        e_ij = closestPoint - transform.position;
        d_ij = e_ij.magnitude;
        // weak repulsion
        float dot = Vector3.Dot(e_ij.normalized, transform.forward);
        Vector3 s_f = transform.forward - (dot * e_ij);
        desiredThetaVector += s_f * fenceWeight;

        if (sheepState == Enums.SheepState.running)
        {
          f_ij = fenceRepulsion / d_ij;
          desiredThetaVector += fenceWeight * f_ij * -e_ij.normalized;
        }
      }
    }

    // interactions only if no dogs
    //    if (dogNeighbours.Count == 0)
    {
      if (sheepState == Enums.SheepState.walking)
      {
        foreach (SheepController neighbour in metricNeighbours)
        {
          desiredThetaVector += neighbour.transform.forward;

          e_ij = neighbour.transform.position - transform.position;
          d_ij = e_ij.magnitude;
          f_ij = Mathf.Min(.0f, (d_ij - r_o) / r_o); // perform only separation to reflect Ginelli model
          desiredThetaVector += beta * f_ij * e_ij.normalized;
        }

        // for sheep with no Metric neighbours set desiredTheta to current forward i.e. no change
        if (metricNeighbours.Count == 0)
          desiredThetaVector += transform.forward;

        // noise
        eps += Random.Range(-Mathf.PI * eta, Mathf.PI * eta);
      }
      else
      if (sheepState == Enums.SheepState.running)
      {
        int topologicNeighboursCount = 0;
        foreach (SheepController neighbour in topologicNeighbours)
        {
          e_ij = neighbour.transform.position - transform.position;
          d_ij = e_ij.magnitude;

          if (d_ij > nearestDogDistance) continue; // ignore neighbours that are further away than the dog when the dog is chasing me

          if (neighbour.sheepState == Enums.SheepState.running)
            desiredThetaVector += neighbour.transform.forward;

          f_ij = Mathf.Min(1.0f, (d_ij - r_e) / r_e);
          desiredThetaVector += beta * ((nearestDogDistance < strongDogRepulsion) ? .5f : 1f) * f_ij * e_ij.normalized; // helps to reduce beta ((nearestDogDistance < strongDogRepulsion)?.5f:1f) as sheep become more aligned when running and less random
          topologicNeighboursCount++;
        }

        // for sheep with no Voronoi neighbours set desiredTheta to current forward i.e. no change
        if (topologicNeighboursCount == 0)
          desiredThetaVector += transform.forward;
      }
      else
      if (sheepState == Enums.SheepState.idle)
      {
        // for idle sheep there is no change ... this could be a also random noise (grazing while idle)
        desiredThetaVector += transform.forward;
      }
    }

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
  }

  void StrombomUpdate()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();

    float rs = 32.5f;
    float ra = 2f;
    int n = 8;

    List<DogController> dogs = new List<DogController>(GM.dogList).Where(dog => (dog.transform.position - transform.position).magnitude < rs).ToList();
    List<SheepController> sheepNeighbours = new List<SheepController>(GM.sheepList).Where(sheepNeighbour => !sheepNeighbour.dead && sheepNeighbour.id != id).ToList();
    //sheepNeighbours = sheepNeighbours.Where(sheepNeighbour => sheep.IsVisible(sheepNeighbour)).ToList();
    sheepNeighbours.Sort(new ByDistanceFrom(transform.position));
    sheepNeighbours = sheepNeighbours.GetRange(0, Mathf.Min(n, sheepNeighbours.Count));

    if (dogs.Count == 0)
    {
      if (Random.Range(.0f, 1.0f) < .05f)
        sheepState = Enums.SheepState.walking;
      else
        sheepState = Enums.SheepState.idle;
    }
    else
      sheepState = Enums.SheepState.running;

    Vector3 Ra = new Vector3();
    Vector3 LCM = new Vector3();
    foreach (SheepController sc in sheepNeighbours)
    {
      if ((transform.position - sc.transform.position).magnitude < ra)
        Ra += (transform.position - sc.transform.position).normalized;
      LCM += sc.transform.position;
    }
    if (sheepNeighbours.Count > 0)
      LCM = LCM / sheepNeighbours.Count;
    else
      LCM = transform.position;

    Vector3 Rs = new Vector3();
    Vector3 Ci = new Vector3();
    foreach (DogController dc in dogs)
    {
      Rs += transform.position - dc.transform.position;
      Ci += LCM - transform.position;
    }

    float e = Random.Range(-Mathf.PI, Mathf.PI);
    desiredThetaVector = .5f * transform.forward + 1.05f * Ci.normalized + 2f * Ra.normalized + 1f * Rs.normalized + .3f * new Vector3(Mathf.Cos(e), 0f, Mathf.Sin(e));

    // fences repulsion
    foreach (Collider fenceCollider in GM.fenceColliders)
    {
      // get dist
      Vector3 closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
      if ((transform.position - closestPoint).sqrMagnitude < fenceRepulsion2)
      {
        Vector3 e_ij = closestPoint - transform.position;
        float d_ij = e_ij.magnitude;
        // weak repulsion
        float dot = Vector3.Dot(e_ij.normalized, transform.forward);
        Vector3 s_f = transform.forward - (dot * e_ij);
        desiredThetaVector += s_f * fenceWeight;

        if (sheepState == Enums.SheepState.running)
        {
          float f_ij = fenceRepulsion / d_ij;
          desiredThetaVector += fenceWeight * f_ij * -e_ij.normalized;
        }
      }
    }

    // extract desired heading
    desiredTheta = Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) * Mathf.Rad2Deg;

    SetSpeed();
  }
}