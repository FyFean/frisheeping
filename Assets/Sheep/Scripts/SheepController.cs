﻿#define DEBUG_ON

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ByDistanceFrom : IComparer<SheepController>, IComparer<DogController>, IComparer<Vector2f>
{
  public Vector3 position { get; set; }
  public ByDistanceFrom(Vector3 pos) { position = pos; }
  public int Compare(SheepController c1, SheepController c2)
  {
    float dc1 = (c1.transform.position - position).sqrMagnitude;
    float dc2 = (c2.transform.position - position).sqrMagnitude;

    if (dc1 > dc2) return 1;
    if (dc1 < dc2) return -1;
    return 0;
  }
  public int Compare(DogController c1, DogController c2)
  {
    float dc1 = (c1.transform.position - position).sqrMagnitude;
    float dc2 = (c2.transform.position - position).sqrMagnitude;

    if (dc1 > dc2) return 1;
    if (dc1 < dc2) return -1;
    return 0;
  }
  public Vector2f point { get; set; }
  public ByDistanceFrom(Vector2f pos) { point = pos; }
  public int Compare(Vector2f p1, Vector2f p2)
  {
    float dc1 = p1.DistanceSquare(point);
    float dc2 = p2.DistanceSquare(point);

    if (dc1 > dc2) return 1;
    if (dc1 < dc2) return -1;
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

  // speed
  private float desiredV = .0f;
  private float v;

  // heading and postion
  private float desiredTheta = .0f;
  private float theta;


  // Ginelli parameters - overriden by GM
  public int n_idle = 0, n_walking = 0, m_toidle = 0, m_idle = 0, m_running = 0;
  public float l_i = .0f;

  // neighbours lists
  [HideInInspector]
  public List<SheepController> metricNeighbours = new List<SheepController>();
  [HideInInspector]
  public List<SheepController> topologicNeighbours = new List<SheepController>();
  [HideInInspector]
  public List<DogController> dogNeighbours = new List<DogController>();

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

    // random state
    sheepState = (Enums.SheepState)Random.Range(0, 3);
    previousSheepState = sheepState;

    // speed
    SetSpeed();
    v = desiredV;

    // random heading
    desiredTheta = Random.Range(-180f, 180f);
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

    // timer
    stateTimer = Random.Range(.0f, stateUpdateInterval);
    drivesTimer = Random.Range(.0f, drivesUpdateInterval);

    Color cottonColor = Color.white;
    // Asign color to sheeps fur

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
        break;
      case Enums.SheepState.walking:
        desiredV = GM.sheepWalkingSpeed;
#if DEBUG_ON
        cottonColor = new Color(1.0f, .5f, .0f, 1.0f);
#endif
        break;
      case Enums.SheepState.running:
        desiredV = GM.sheepRunningSpeed;
#if DEBUG_ON
        cottonColor = new Color(.0f, 1.0f, .0f, 1.0f);
#endif
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

    if (GM.StrombomSheep)
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
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, GM.sheepMaxTurn * Time.deltaTime);
    // ensure angle remains in [-180,180)
    theta = (theta + 180f) % 360f - 180f;
    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, GM.sheepMaxSpeedChange * Time.deltaTime);

    // update position
    Vector3 newPosition = transform.position + (Time.deltaTime * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    // Sheep state animation
    anim.SetBool("IsIdle", sheepState == Enums.SheepState.idle);
    anim.SetBool("IsRunning", sheepState == Enums.SheepState.running);

#if false
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

  void NeighboursUpdate()
  {
    // executed globaly in GM to achieve a higher update rate, changes due to asynchronous execution ignored
  }

  void UpdateState()
  {
    float dt = stateUpdateInterval;
    if (stateUpdateInterval <= 0f)
      dt = Time.deltaTime;

    previousSheepState = sheepState;

    float nd = GM.SheepParametersGinelli.r_s * GM.SheepParametersGinelli.r_s; // nearest dog distance
    // if dog detected switch state differently
    // if (dogNeighbours.Count > 0)
    { // this could be coded like piwr & pri the l_i/d_R, d_S/l_i parts, but l_i being the mean distance to the dog(s)
      // dog repulsion
      foreach (DogController dog in dogNeighbours)
      {
        float dist = (dog.transform.position - transform.position).sqrMagnitude;

#if false // experiment with dogRepulsion not forcing state change
        // override state and speed
        // running when dog is close, walking when dog is medium distance away
        if (dist < strongDogRepulsion)
          sheepState = Enums.SheepState.running;
        else if (sheepState != Enums.SheepState.running)
          sheepState = Enums.SheepState.walking;
#endif
        nd = Mathf.Min(nd, dist); 
      }
    }
    // else
    {
      // probabilities
      float p_iw = (1 + GM.SheepParametersGinelli.alpha * n_walking) / GM.SheepParametersGinelli.tau_iw;
      p_iw = 1 - Mathf.Exp(-p_iw * dt);
      float p_wi = (1 + GM.SheepParametersGinelli.alpha * n_idle) / GM.SheepParametersGinelli.tau_wi;
      p_wi = 1 - Mathf.Exp(-p_wi * dt);

      float p_iwr = .0f;
      float p_ri = 1.0f; // since l_i is in the denominator of the eq for p_ri
      if (l_i > .0f)
      {
        float d_R = GM.SheepParametersGinelli.d_R;
#if true // experiment with dogRepulsion not forcing state change        
        if (nd < GM.SheepParametersGinelli.r_sS) // feel unsafe much sooner
          d_R *= .1f;
        else if (nd < GM.SheepParametersGinelli.r_s) // feel unsafe sooner
          d_R *= .5f;
#endif
        p_iwr = (1 / GM.SheepParametersGinelli.tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + GM.SheepParametersGinelli.alpha * m_running), GM.SheepParametersGinelli.delta);
        p_iwr = 1 - Mathf.Exp(-p_iwr * dt);

#if true // experiment with dogRepulsion not forcing state change
//        if (d_i < strongDogRepulsion) // feel unsafe much sooner
//          d_S *= .1f;
//        else if (d_i < dogRepulsion) // feel unsafe sooner
//          d_S = d_S * .75f;
#endif
        p_ri = (1 / GM.SheepParametersGinelli.tau_ri) * Mathf.Pow((GM.SheepParametersGinelli.d_S / l_i) * (1 + GM.SheepParametersGinelli.alpha * m_toidle), GM.SheepParametersGinelli.delta);
        p_ri = 1 - Mathf.Exp(-p_ri * dt);
      }

      if (nd < GM.SheepParametersGinelli.d_S) // r_0
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
      //else // added to reflect SheepOptimization code
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

    // dog repulsion regardless of state
    foreach (DogController dog in dogNeighbours)
    {
      e_ij = dog.transform.position - transform.position;
      d_ij = e_ij.magnitude;

      float p_iw = 1f - d_ij / GM.SheepParametersGinelli.r_s;
      float p_iwr = 1f - Mathf.Min(1f, d_ij / GM.SheepParametersGinelli.r_sS);
      if (sheepState == Enums.SheepState.idle)
      {
        if (Random.Range(.0f, 1.0f) < p_iw)
        {
          sheepState = Enums.SheepState.walking;
          SetSpeed();
        }
      }
      if (sheepState == Enums.SheepState.walking)
      {
        if (Random.Range(.0f, 1.0f) < p_iwr)
        {
          sheepState = Enums.SheepState.running;
          SetSpeed();
        }
      }
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
      f_ij = Mathf.Min(1f, (d_ij - GM.SheepParametersGinelli.r_s) / GM.SheepParametersGinelli.r_s);
      desiredThetaVector += GM.SheepParametersGinelli.rho_s * f_ij * e_ij.normalized;
    }

    if (sheepState != Enums.SheepState.idle)
    {
      // repulsion from fences and trees
      float r_f2 = GM.SheepParametersGinelli.r_f * GM.SheepParametersGinelli.r_f;
      foreach (Collider fenceCollider in GM.fenceColliders)
      {
        Vector3 closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
        if ((transform.position - closestPoint).sqrMagnitude < r_f2)
        {
          e_ij = closestPoint - transform.position;
          d_ij = e_ij.magnitude;

          f_ij = Mathf.Min(.0f, (d_ij - GM.SheepParametersGinelli.r_f) / GM.SheepParametersGinelli.r_f);
          desiredThetaVector += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;

#if false // should be handled in state transitions
          // if walking transition to idle mode the closer to the fence the more likely
          if (sheepState == Enums.SheepState.walking)
            if (Random.Range(.0f, 1.0f) < 1f - (d_ij / GM.SheepParametersGinelli.r_f))
              sheepState = Enums.SheepState.idle;
#endif
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

          // ignore separation
#if true
          e_ij = neighbour.transform.position - transform.position;
          d_ij = e_ij.magnitude;
          f_ij = Mathf.Min(.0f, (d_ij - GM.SheepParametersGinelli.r_0) / GM.SheepParametersGinelli.r_0); // perform only separation to reflect Ginelli model
          desiredThetaVector += GM.SheepParametersGinelli.beta * f_ij * e_ij.normalized;
#endif
        }

        // for sheep with no Metric neighbours set desiredTheta to current forward i.e. no change
        if (metricNeighbours.Count == 0 && desiredThetaVector.sqrMagnitude == 0)
          desiredThetaVector += transform.forward;

        // noise
        eps += Random.Range(-Mathf.PI * GM.SheepParametersGinelli.eta, Mathf.PI * GM.SheepParametersGinelli.eta);
      }
      else
      if (sheepState == Enums.SheepState.running)
      {
        foreach (SheepController neighbour in topologicNeighbours)
        {
          e_ij = neighbour.transform.position - transform.position;
          d_ij = e_ij.magnitude;

//          if (d_ij > nearestDogDistance) continue; // ignore neighbours that are further away than the dog when the dog is chasing me

          if (neighbour.sheepState == Enums.SheepState.running)
            desiredThetaVector += neighbour.transform.forward;

          f_ij = Mathf.Min(1.0f, (d_ij - GM.SheepParametersGinelli.r_e) / GM.SheepParametersGinelli.r_e);
          desiredThetaVector += GM.SheepParametersGinelli.beta * f_ij * e_ij.normalized;

//          desiredThetaVector += beta * ((nearestDogDistance < strongDogRepulsion) ? .5f : 1f) * f_ij * e_ij.normalized; // helps to reduce beta ((nearestDogDistance < strongDogRepulsion)?.5f:1f) as sheep become more aligned when running and less random
        }

        // for sheep with no Voronoi neighbours set desiredTheta to current forward i.e. no change
        if (topologicNeighbours.Count == 0 && desiredThetaVector.sqrMagnitude == 0)
          desiredThetaVector += transform.forward;
      }
      else
      if (sheepState == Enums.SheepState.idle)
      {
        // for idle sheep there is no change 
        desiredThetaVector += transform.forward;

        // random noise (grazing while idle)
        eps += Random.Range(-Mathf.PI * GM.SheepParametersGinelli.eta, Mathf.PI * GM.SheepParametersGinelli.eta);
      }
    }

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
  }

  public bool IsVisible(SheepController sc, float blindAngle)
  {
    return IsVisible(sc.GetComponent<Rigidbody>(), blindAngle);
  }

  public bool IsVisible(DogController dc, float blindAngle)
  {
    return IsVisible(dc.GetComponent<Rigidbody>(), blindAngle);
  }

  public bool IsVisible(Rigidbody rb, float blindAngle)
  {
#if true // experimental: test occlusion
    Vector3 toCm = rb.worldCenterOfMass - GetComponent<Rigidbody>().worldCenterOfMass;
    bool hit = Physics.Raycast(GetComponent<Rigidbody>().worldCenterOfMass + .5f * toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
    Vector3 toSc = rb.transform.position - transform.position;
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }

  void StrombomUpdate()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();

    List<DogController> dogs = new List<DogController>(GM.dogList).Where(dog => (dog.transform.position - transform.position).sqrMagnitude < GM.SheepParametersStrombom.r_s * GM.SheepParametersStrombom.r_s).ToList();
    if (GM.SheepParametersStrombom.occlusion)
      dogs = dogs.Where(dog => IsVisible(dog, GM.SheepParametersStrombom.blindAngle)).ToList();
    dogs.Sort(new ByDistanceFrom(transform.position));
    dogs = dogs.GetRange(0, Mathf.Min(GM.SheepParametersStrombom.n, dogs.Count));
    List<SheepController> sheepNeighbours = new List<SheepController>(GM.sheepList).Where(sheepNeighbour => !sheepNeighbour.dead && sheepNeighbour.id != id).ToList();
    if (GM.SheepParametersStrombom.occlusion)
      sheepNeighbours = sheepNeighbours.Where(sheepNeighbour => sheepNeighbour.IsVisible(sheepNeighbour, GM.SheepParametersStrombom.blindAngle)).ToList();
    sheepNeighbours.Sort(new ByDistanceFrom(transform.position));
    sheepNeighbours = sheepNeighbours.GetRange(0, Mathf.Min(GM.SheepParametersStrombom.n, sheepNeighbours.Count));

    if (dogs.Count == 0)
    {
      if (Random.Range(.0f, 1.0f) < .05f)
        sheepState = Enums.SheepState.walking;
      else
        sheepState = Enums.SheepState.idle;
    }
    else
    {
      // are there any dogs very close
      if (new List<DogController>(dogs).Where(dog => (dog.transform.position - transform.position).magnitude < GM.SheepParametersStrombom.r_sS).ToList().Count > 0)
        sheepState = Enums.SheepState.running;
      else
        sheepState = Enums.SheepState.walking;
    }

    if (sheepState != Enums.SheepState.idle)
    {
      // repulsion directly from shepherds
      Vector3 Rs = new Vector3();
      foreach (DogController dc in dogs)
        Rs += transform.position - dc.transform.position;

      Vector3 Ra = new Vector3();
      Vector3 LCM = new Vector3();
      foreach (SheepController sc in sheepNeighbours)
      {
        // repulsion from interacting neighbours
        if ((transform.position - sc.transform.position).magnitude < GM.SheepParametersStrombom.r_a)
          Ra += (transform.position - sc.transform.position).normalized;
        LCM += sc.transform.position;
      }
      LCM += transform.position;
      LCM = LCM / (sheepNeighbours.Count + 1);

      // attraction towards LCM
      Vector3 Ci = new Vector3();
      Ci += LCM - transform.position;

      // noise
      float eps = Random.Range(-Mathf.PI * GM.SheepParametersStrombom.e, Mathf.PI * GM.SheepParametersStrombom.e);
      desiredThetaVector += GM.SheepParametersStrombom.h * transform.forward + 
        GM.SheepParametersStrombom.c * Ci.normalized + 
        GM.SheepParametersStrombom.rho_a * Ra.normalized + 
        GM.SheepParametersStrombom.rho_s * Rs.normalized;

      // repulsion from fences and trees
      float r_f2 = GM.SheepParametersStrombom.r_f * GM.SheepParametersStrombom.r_f;
      foreach (Collider fenceCollider in GM.fenceColliders)
      {
        Vector3 closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
        if ((transform.position - closestPoint).sqrMagnitude < r_f2)
        {
          Vector3 e_ij = closestPoint - transform.position;
          float d_ij = e_ij.magnitude;

          float f_ij = Mathf.Min(.0f, (d_ij - GM.SheepParametersStrombom.r_f) / GM.SheepParametersStrombom.r_f);
          desiredThetaVector += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;

          // if walking transition to idle mode the closer to the fence the more likely
          if (sheepState == Enums.SheepState.walking)
            if (Random.Range(.0f, 1.0f) < 1f - (d_ij / GM.SheepParametersStrombom.r_f))
              sheepState = Enums.SheepState.idle;
        }
      }

      // extract desired heading
      desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
    }

    SetSpeed();
  }
}