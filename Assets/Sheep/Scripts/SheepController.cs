﻿#define DEBUG_ON

using UnityEngine;
using System.Collections.Generic;

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
  private float maxTurn = .07f/.016f;

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

  // neighbour interaction
  private float r_o = 1f;//1.5f; // original 1.0f
  private float r_e = 1f;//1.5f; // original 1.0f
  [HideInInspector]
  public float r_o2;

  // speed
  private float maxSpeedChange = .1f/.016f;
  private float desiredV = .0f;
  private float v;
  private float v_1 = 1.0f; // original 0.15f
  private float v_2 = 7.5f; // original 1.5f

  // noise
  private float eta = 0.2f; // original 0.13f

  // beta - cohesion factor
  private float beta = .8f;//3.0f; // original 0.8f

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
  private float stateUpdateInterval = .5f;
  private float stateTimer;
  private float drivesUpdateInterval = .2f;
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
    desiredTheta = Random.Range(-Mathf.PI, Mathf.PI);
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;

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
    /* end of behaviour logic */

    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(Mathf.Rad2Deg * theta, Mathf.Rad2Deg * desiredTheta, Mathf.Rad2Deg * maxTurn * Time.deltaTime) * Mathf.Deg2Rad;
    // ensure angle remains in [-Pi,Pi)
    theta = (theta + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, maxSpeedChange * Time.deltaTime);

    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;

    // update position
    Vector3 newPosition = transform.position + (Time.deltaTime * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    // Sheep state animation
    anim.SetBool("IsIdle", sheepState == Enums.SheepState.idle);
    anim.SetBool("IsRunning", sheepState == Enums.SheepState.running);
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
    previousSheepState = sheepState;

//    float d_i = dogRepulsion2; // min distance to dogNeighbours
    // if dog detected switch state differently
    if (dogNeighbours.Count > 0)
    { // this could be coded like piwr & pri the l_i/d_R, d_S/l_i parts, but l_i being the mean distance to the dog(s)
      // dog repulsion
      foreach (DogController DC in dogNeighbours)
      {
        float dist = (DC.transform.position - transform.position).magnitude;

        // override state and speed
        // running when dog is close, walking when dog is medium distance away
        if (dist < strongDogRepulsion)
          sheepState = Enums.SheepState.running;
        else if (sheepState != Enums.SheepState.running)
          sheepState = Enums.SheepState.walking;
//        d_i = Mathf.Min(d_i, dist); 
      }
    }
    else
    {
      // set transition parameters in case sheep number changes
      tau_iwr = GM.sheepCount;
      tau_ri = GM.sheepCount;

      // probabilities
      float p_iw = (1 + alpha * n_walking) / tau_iw;
      p_iw = 1 - Mathf.Exp(-p_iw * stateUpdateInterval);
      float p_wi = (1 + alpha * n_idle) / tau_wi;
      p_wi = 1 - Mathf.Exp(-p_wi * stateUpdateInterval);
      float p_iwr = .0f;
      float p_ri = 1.0f; // since l_i is in the denominator of the eq for p_ri

      if (l_i > .0f)
      {
#if false // experiment with dogRepulsion not forcing state change
        if (d_i < dogRepulsion) // feel unsafe sooner
          p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / (d_R*.75f)) * (1 + alpha * m_running), delta);
        else if (d_i < strongDogRepulsion) // feel unsafe much sooner
          p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / (d_R*.1f)) * (1 + alpha * m_running), delta);
        else 
#endif
        p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + alpha * m_running), delta);
        p_iwr = 1 - Mathf.Exp(-p_iwr * stateUpdateInterval);

#if false // experiment with dogRepulsion not forcing state change
        if (d_i < dogRepulsion) // feel safe later
          p_ri = (1 / tau_ri) * Mathf.Pow(((d_S*.75f) / l_i) * (1 + alpha * m_toidle), delta);
        else if (d_i < strongDogRepulsion) // feel safe much sooner
          p_ri = (1 / tau_ri) * Mathf.Pow(((d_S*.1f) / l_i) * (1 + alpha * m_toidle), delta);
        else
#endif
        p_ri = (1 / tau_ri) * Mathf.Pow((d_S / l_i) * (1 + alpha * m_toidle), delta);
        p_ri = 1 - Mathf.Exp(-p_ri * stateUpdateInterval);
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
    Vector3 e_ij, s_f;
    float f_ij, dot;

    // dog repulsion
    foreach (DogController DC in dogNeighbours)
    {
      e_ij = DC.transform.position - transform.position;

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

      f_ij = dogRepulsion / Vector3.Magnitude(e_ij);
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
        // weak repulsion
        dot = Vector3.Dot(e_ij.normalized, transform.forward);
        s_f = transform.forward - (dot * e_ij);
        desiredThetaVector += s_f * fenceWeight;

        if (sheepState == Enums.SheepState.running)
        {
          f_ij = fenceRepulsion / Vector3.Magnitude(e_ij);
          desiredThetaVector += fenceWeight * f_ij * -e_ij.normalized;
        }
      }
    }

    // interactions only if no dogs
    if (dogNeighbours.Count == 0)
    {
      if (sheepState == Enums.SheepState.walking)
      {
        foreach (SheepController neighbour in metricNeighbours)
        {
          desiredThetaVector += neighbour.transform.forward;

          e_ij = neighbour.transform.position - transform.position;
          f_ij = Mathf.Min(.0f, ((Vector3.Magnitude(e_ij) - r_o) / r_o)); // perform only separation to reflect Ginelli model
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
        foreach (SheepController neighbour in topologicNeighbours)
        {
          if (neighbour.sheepState == Enums.SheepState.running)
            desiredThetaVector += neighbour.transform.forward;

          e_ij = neighbour.transform.position - transform.position;
          f_ij = Mathf.Min(1.0f, ((Vector3.Magnitude(e_ij) - r_e) / r_e));
          desiredThetaVector += beta * f_ij * e_ij.normalized;
        }

        // for sheep with no Voronoi neighbours set desiredTheta to current forward i.e. no change
        if (topologicNeighbours.Count == 0)
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
    desiredTheta = Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps;
  }
}