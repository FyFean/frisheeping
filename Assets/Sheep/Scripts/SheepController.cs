#define DEBUG_ON

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ByDistanceFrom : IComparer<SheepController>, IComparer<DogController>, IComparer<Vector2f>
{
    public Vector3 position { get; set; }
    public SheepController sc { get; set; }
    private bool usePosition = false;
    private GameManager GM;
    public ByDistanceFrom(Vector3 pos) { position = pos; usePosition = true; }
    public ByDistanceFrom(SheepController s) { sc = s; GM = s.GM; }
#if false // call transform.position
public int Compare(SheepController c1, SheepController c2)
{
    float dc1 = (c1.transform.position - position).sqrMagnitude;
    float dc2 = (c2.transform.position - position).sqrMagnitude;

    if (dc1 > dc2) return 1;
    if (dc1 < dc2) return -1;
    return 0;
}
#else // use cached position
    public int Compare(SheepController c1, SheepController c2)
    {
        float dc1, dc2;
        if (usePosition)
        {
            dc1 = (c1.position - position).sqrMagnitude;
            dc2 = (c2.position - position).sqrMagnitude;
        }
        else
        {
            dc1 = GM.sheepDistances[sc.id, c1.id];
            dc2 = GM.sheepDistances[sc.id, c1.id];
        }




        if (dc1 > dc2) return 1;
        if (dc1 < dc2) return -1;
        return 0;
    }
#endif
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

    // State of the sheep - speed enum
    [HideInInspector]
    public Enums.SheepState sheepState;
    public Enums.SheepState previousSheepState;

    // Sheeps Animator Controller
    public Animator anim;
    public FuzzyLogic fuzzyLogic;
    public FuzzyLogicMovement fuzzyLogicMovement;

    // Fur parts
    public Renderer[] sheepCottonParts;

    // GameManager
    //private GameManager GM;
    public GameManager GM;

    // speed
    private float desiredV = .0f;
    private float v;

    // heading and postion
    private float desiredTheta = .0f;
    private float theta;

    // 
    public float getTheta() {
        return this.theta;
    }


    // Ginelli parameters - overriden by GM
    public float n_idle = .0f, n_walking = .0f, m_toidle = .0f, m_idle = .0f, m_running = .0f;
    public float l_i = .0f;

    // neighbours lists
    [HideInInspector]
    public List<SheepController> metricNeighbours = new List<SheepController>();
    [HideInInspector]
    public List<SheepController> topologicNeighbours = new List<SheepController>();
    [HideInInspector]
    public List<DogController> dogNeighbours = new List<DogController>();

    // update timers
    private float stateUpdateInterval = 0 * .5f;//2*.5f;
    private float stateTimer;
    private float drivesUpdateInterval = 0 * .02f;//1*.2f;
    private float drivesTimer;

    // dead flag
    [HideInInspector]
    public bool dead = false;

    // cached position
    [HideInInspector]
    public Vector3 position = new Vector3();

    void Start()
    {
        // GameManager
        GM = FindObjectOfType<GameManager>();

        this.fuzzyLogic = new FuzzyLogic(this.id);
        this.fuzzyLogicMovement = new FuzzyLogicMovement(this.id);

        // random state
        sheepState = (Enums.SheepState)Random.Range(0, 3);
        if (sheepState == Enums.SheepState.running) sheepState = Enums.SheepState.walking;
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

        if (this.id == 15) {
            cottonColor = new Color(1f, 0f, 0f, 1.0f);
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
        if (this.id == 15)
        {
            cottonColor = new Color(1f, 0f, 0f, 1.0f);
        }

#if DEBUG_ON
        foreach (Renderer fur in sheepCottonParts)
        {
            if (fur.materials.Length < 2) fur.material.color = cottonColor;
            else fur.materials[1].color = cottonColor;
        }
#endif
    }

    float CalculateAverageDegree(float angle1, float angle2)
    {
        // Normalize angles to the range of -180 to 180 degrees
        angle1 = NormalizeAngle(angle1);
        angle2 = NormalizeAngle(angle2);

        // Calculate the average
        float average = (angle1 + angle2) / 2f;

        // Normalize the average angle
        return NormalizeAngle(average);
    }

    float NormalizeAngle(float angle)
    {
        // Normalize angle to the range of -180 to 180 degrees
        while (angle < -180)
            angle += 360;
        while (angle > 180)
            angle -= 360;

        return angle;
    }

    private void FixedUpdate()
    {
        if (!GM.useFixedTimestep) Move();
    }

    void Move()
    {
        float timestep;
        if (GM.useFixedTimestep)
        {
            timestep = GM.fixedTimestep;
        }
        else
        {
            timestep = Time.deltaTime;
        }
        // compute angular change based on max angular velocity and desiredTheta
        theta = Mathf.MoveTowardsAngle(theta, desiredTheta, GM.sheepMaxTurn * timestep);
        // ensure angle remains in [-180,180)
        theta = (theta + 180f) % 360f - 180f;

        // compute new forward direction
        Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

        // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
        v = Mathf.MoveTowards(v, desiredV, GM.sheepMaxSpeedChange * timestep);

        // update position
        Vector3 newPosition = transform.position + (timestep * v * newForward);
        // force ground, to revert coliders making sheep fly
        newPosition.y = 0f;

        transform.position = newPosition;
        transform.forward = newForward;

        // update distance values after position update
        for (int i = 0; i < GM.sheepCount; i++)
        {
            SheepController sc = GM.sheepList[i];
            GM.sheepDistances[this.id, sc.id] = (sc.position - this.position).sqrMagnitude;
            GM.sheepDistances[sc.id, this.id] = GM.sheepDistances[this.id, sc.id];
        }
    }

    void Update()
    {
        float timestep;
        if (GM.useFixedTimestep)
        {
            timestep = GM.fixedTimestep;
        }
        else
        {
            timestep = Time.deltaTime;
        }
        UnityEngine.Profiling.Profiler.BeginSample("SheepUpdate");
        /* behavour logic */
        drivesTimer -= timestep;
        stateTimer -= timestep;

        if (GM.StrombomSheep)
        {
            // drives update
            if (drivesTimer < 0)
            {
                //StrombomUpdate();
                FuzzyUpdate();
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
        UnityEngine.Profiling.Profiler.EndSample();
        if (GM.useFixedTimestep) Move();
    }

    void NeighboursUpdate()
    {
        // executed globaly in GM to achieve a higher update rate, changes due to asynchronous execution ignored
    }

    void UpdateState()
    {
        float timestep;
        if (GM.useFixedTimestep)
        {
            timestep = GM.fixedTimestep;
        }
        else
        {
            timestep = Time.deltaTime;
        }
        float dt = stateUpdateInterval;
        if (stateUpdateInterval <= 0f)
            dt = timestep;

        previousSheepState = sheepState;

        // increment m_running if dog in strong repulsion zone
        float d_fear = .0f;
        float nd = Mathf.Infinity; // nearest dog distance
        foreach (DogController dog in dogNeighbours)
        {
            float dist = (dog.transform.position - transform.position).magnitude;
            nd = Mathf.Min(nd, dist);
            d_fear += Mathf.Pow(Mathf.Max(.0f, 1f - dist / GM.SheepParametersGinelli.r_sS), 2f);
        }
        if (dogNeighbours.Count > 0)
            d_fear /= dogNeighbours.Count;

        float d_R = GM.SheepParametersGinelli.d_R;
        float d_S = GM.SheepParametersGinelli.d_S;
#if true // experiment with dogRepulsion not forcing state change, could be based also on d_fear        
        if (nd < GM.SheepParametersGinelli.r_sS) // feel unsafe much sooner
            d_R *= .25f * Mathf.Pow(nd / GM.SheepParametersGinelli.r_sS, 2f);
        else if (nd < GM.SheepParametersGinelli.r_s) // feel unsafe sooner
            d_R *= .25f + .25f * Mathf.Pow((nd - GM.SheepParametersGinelli.r_sS) / (GM.SheepParametersGinelli.r_s - GM.SheepParametersGinelli.r_sS), 2f);
#endif
#if true // experiment with dogRepulsion not forcing state change
        if (nd < GM.SheepParametersGinelli.r_sS) // feel unsafe much sooner
            d_S *= .25f * Mathf.Pow(nd / GM.SheepParametersGinelli.r_sS, 2f);
        else if (nd < GM.SheepParametersGinelli.r_s) // feel unsafe sooner
            d_S *= .25f + .25f * Mathf.Pow((nd - GM.SheepParametersGinelli.r_sS) / (GM.SheepParametersGinelli.r_s - GM.SheepParametersGinelli.r_sS), 2f);
#endif
#if UNITY_EDITOR
    if (UnityEditor.Selection.activeGameObject != null && UnityEditor.Selection.activeGameObject.GetComponent<SheepController>() != null && UnityEditor.Selection.activeGameObject.GetComponent<SheepController>().id == id)
    {
      Debug.DrawCircle(transform.position, l_i, new Color(1f, 1f, 1f, 1f));
      Debug.DrawCircle(transform.position, d_R, new Color(0f, 0f, 1f, 1f));
      Debug.DrawCircle(transform.position, d_S, new Color(1f, 0f, 1f, 1f));
      foreach (SheepController snt in topologicNeighbours)
        Debug.DrawCircle(snt.transform.position, .5f, new Color(1f, 1f, 1f, 1f));
    }
#endif
        // probabilities
        float p_iw = (1 + GM.SheepParametersGinelli.alpha * n_walking) / GM.SheepParametersGinelli.tau_iw;
        p_iw = 1 - Mathf.Exp(-p_iw * dt);
        float p_wi = (1 + GM.SheepParametersGinelli.alpha * n_idle) / GM.SheepParametersGinelli.tau_wi;
        p_wi = 1 - Mathf.Exp(-p_wi * dt);

        float p_iwr = .0f;
        float p_ri = 1.0f; // since l_i is in the denominator of the eq for p_ri
        if (l_i > .0f)
        {
            p_iwr = (1 / GM.SheepParametersGinelli.tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + GM.SheepParametersGinelli.alpha * (m_running + d_fear)), GM.SheepParametersGinelli.delta);
            p_iwr = 1 - Mathf.Exp(-p_iwr * dt);

            p_ri = (1 / GM.SheepParametersGinelli.tau_ri) * Mathf.Pow((d_S / l_i) * (1 + GM.SheepParametersGinelli.alpha * m_toidle), GM.SheepParametersGinelli.delta);
            p_ri = 1 - Mathf.Exp(-p_ri * dt);
        }
        else // no topologic neighbours but dog nearby
        {
            p_iwr = .25f + .75f * (1f - Mathf.Pow(nd / GM.SheepParametersGinelli.r_sS, 2f));
            p_ri = 1f - p_iwr;
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
        // else // added to reflect SheepOptimization code
        // while testing the transition to running also test the transition from running to standing
        if (sheepState == Enums.SheepState.running)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < p_ri)
                sheepState = Enums.SheepState.idle;
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
        float ndc = Mathf.Infinity;
        foreach (DogController dog in dogNeighbours)
        {
            e_ij = dog.transform.position - transform.position;
            d_ij = e_ij.magnitude;

            ndc = Mathf.Min(ndc, d_ij);

            f_ij = Mathf.Min(1f, (d_ij - GM.SheepParametersGinelli.r_s) / GM.SheepParametersGinelli.r_s);
            // TODO: intensify repulsion if dog is going streight for me
            // f_ij *= Mathf.Max(0f, Vector3.Dot(dog.transform.forward, e_ij.normalized));
            desiredThetaVector += GM.SheepParametersGinelli.rho_s * f_ij * e_ij.normalized;
        }

        if (sheepState != Enums.SheepState.idle)
        {
            // repulsion from fences and trees
            float r_f2 = GM.SheepParametersGinelli.r_f * GM.SheepParametersGinelli.r_f;
            foreach (Collider fenceCollider in GM.fenceColliders)
            {
                Vector3 closestPoint = fenceCollider.ClosestPoint(transform.position);
                if ((transform.position - closestPoint).sqrMagnitude < r_f2)
                {
                    e_ij = closestPoint - transform.position;
                    d_ij = e_ij.magnitude;

                    f_ij = Mathf.Min(.0f, (d_ij - GM.SheepParametersGinelli.r_f) / GM.SheepParametersGinelli.r_f);
                    desiredThetaVector += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;

#if false // TODO: should be handled in state transitions
          // if walking transition to idle mode the closer to the fence the more likely
          if (sheepState == Enums.SheepState.walking)
            if (Random.Range(.0f, 1.0f) < 1f - (d_ij / GM.SheepParametersGinelli.r_f))
              sheepState = Enums.SheepState.idle;
#endif
                }
            }
        }
        if (sheepState == Enums.SheepState.walking)
        {
            foreach (SheepController neighbour in metricNeighbours)
            {
                desiredThetaVector += neighbour.transform.forward;

#if true // include separation
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

                //          if (d_ij > ndc) continue; // ignore neighbours that are further away than the dog when the dog is chasing me

                if (neighbour.sheepState == Enums.SheepState.running)
                {
                    f_ij = 1f;
                    // reduce influence of neighbours that are furhter away
                    // f_ij *= Mathf.Exp(-Mathf.Max(.0f, d_ij - l_i));
                    // f_ij = Mathf.Max(.0f, 1f - d_ij / l_i); revise
                    desiredThetaVector += f_ij * neighbour.transform.forward;
                }

                f_ij = Mathf.Min(1.0f, (d_ij - GM.SheepParametersGinelli.r_e) / GM.SheepParametersGinelli.r_e);
                // f_ij *= Mathf.Exp(-Mathf.Max(.0f, d_ij - l_i)); // reduce influeence of neighbours that are furhter away
                desiredThetaVector += GM.SheepParametersGinelli.beta * f_ij * e_ij.normalized;
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
#if false // experimental: test occlusion
    Vector3 toCm = rb.worldCenterOfMass - GetComponent<Rigidbody>().worldCenterOfMass;
    bool hit = Physics.Raycast(GetComponent<Rigidbody>().worldCenterOfMass + .5f * toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
        Vector3 toSc = rb.transform.position - transform.position;
        float cos = Vector3.Dot(transform.forward, toSc.normalized);
        return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
    }

    //TODO: dodaj tale nas update function v kodo (tam kjer se klice strombom) in poglej, kako se spreminja speed/... 
    // glede na nasi 2 vrednosti
    void FuzzyUpdate()
    {

        Vector3 desiredThetaVector = new Vector3();
        var dogs = GM.dogList.Where(dog => (dog.transform.position - transform.position).sqrMagnitude < GM.SheepParametersStrombom.r_s * GM.SheepParametersStrombom.r_s);
        if (GM.SheepParametersStrombom.occlusion)
            dogs = dogs.Where(dog => IsVisible(dog, GM.SheepParametersStrombom.blindAngle));
        dogs = dogs.OrderBy(d => d, new ByDistanceFrom(transform.position))
            .Take(GM.SheepParametersStrombom.n);
        var sheepNeighbours = GM.sheepList.Where(sheepNeighbour => !sheepNeighbour.dead && sheepNeighbour.id != id && sheepNeighbour.sheepState != Enums.SheepState.idle);
        if (GM.SheepParametersStrombom.occlusion)
            sheepNeighbours = sheepNeighbours.Where(sheepNeighbour => sheepNeighbour.IsVisible(sheepNeighbour, GM.SheepParametersStrombom.blindAngle));
#if false // call transform.position
    sheepNeighbours = sheepNeighbours
            .OrderBy(d => d, new ByDistanceFrom(transform.position))
            .Take(GM.SheepParametersStrombom.n);
#else // use cached position
        sheepNeighbours = sheepNeighbours
                .OrderBy(d => d, new ByDistanceFrom(this))
                .Take(5);
#endif
        Vector3[] SheepPos = SheepUtils.GetSheepPositions(sheepNeighbours);
        Vector3[] DogPos = SheepUtils.GetDogPositions(dogs);
        float[] sheepDist = SheepUtils.CalculateDistances(this.position, SheepPos);
        float[] dogDist = SheepUtils.CalculateDistances(this.position, DogPos);
        float[] sheepAng = SheepUtils.CalculateAngles(this.transform, sheepNeighbours);
        float curr_speed = SheepUtils.SpeedEnumtoFloat(this.sheepState);
        float[] sheepSpeeds = SheepUtils.GetSheepSpeeds(curr_speed, sheepNeighbours);

        float[] fuzzy_values = fuzzyLogicMovement.fuzzyfy(this.theta, sheepDist, dogDist, sheepAng, sheepSpeeds);
        fuzzy_values[1] = ((fuzzy_values[1]- 180) % 360);

        Vector3 Rs = new Vector3();
        float R_mag = 0f;
        foreach (DogController dc in dogs)
        {
            Rs += (transform.position - dc.transform.position);
            R_mag += (dc.transform.position - transform.position).magnitude;
        }

        float stateFloat = 0.5f;

        if (R_mag > 0f)
        {
            if (R_mag < 12.5f)
            {
                stateFloat = 2.5f;
            }
            else { 
            stateFloat = 1.5f;
            }

            //stateFloat = 0.8f * stateFloat + 0.2f * fuzzy_values[0];
        }
        //else
        //{
        //    stateFloat = 1f;
        //}

        this.sheepState = SheepUtils.FloatToSpeedEnum(stateFloat);

        float avg_dog_d = SheepUtils.CalculateAverage(dogDist);
        float avg_s_d = SheepUtils.CalculateAverage(sheepAng);
        Vector3 dog_rep = GM.SheepParametersStrombom.h * transform.forward + 3f * Rs.normalized;
        float dogTheta = (Mathf.Atan2(dog_rep.z, dog_rep.x)) * Mathf.Rad2Deg;


        //if (fuzzy_values[1] < 0.5 && fuzzy_values[1] > -0.5) 
        //{
        //    this.desiredTheta = dogTheta;

        //}
        //else if (avg_dog_d < 12.5f)
        //{
        //    this.desiredTheta = 0.9f * dogTheta + 0.1f * (fuzzy_values[1]);
        //}
        //else
        //{
        this.desiredTheta = CalculateAverageDegree(dogTheta, fuzzy_values[1]);
        //}
        //this.desiredTheta = dogTheta;
        if (this.id == 15)
        {
            //Debug.Log("final test 1 (" + sheepAng.Length + "): " + string.Join(", ", sheepAng));
            //Debug.Log("final test 2 (" + sheepSpeeds.Length + "): " + string.Join(", ", sheepDist));
            //Debug.Log("final test (" + string.Join(", ", fuzzy_values) + ")" + this.sheepState + " " + this.desiredTheta);
            //Debug.Log("final test: " + this.transform.position + " | " + stateFloat + " " + this.sheepState + " " + R_mag + " | " + ", " + dog_rep + " " + Rs + " " + this.desiredTheta);
        }
        //Debug.Log("final test 12: " + this.desiredTheta + " | " + fuzzy_values[1]+ " "+dogTheta +"  " + this.theta);

        //if (dogs.Count() == 0)
        //{
        //    if (Random.Range(.0f, 1.0f) < .05f)
        //        sheepState = Enums.SheepState.walking;
        //    else
        //        sheepState = Enums.SheepState.idle;
        //}
        //else
        //{
        //    // are there any dogs very close
        //    if (dogs.Where(dog => (dog.transform.position - transform.position).magnitude < GM.SheepParametersStrombom.r_sS).Count() > 0)
        //        sheepState = Enums.SheepState.running;
        //    else
        //        sheepState = Enums.SheepState.walking;
        //}

        //if (sheepState != Enums.SheepState.idle)
        //{
        //    // repulsion directly from shepherds
        //    Vector3 Rs = new Vector3();
        //    foreach (DogController dc in dogs)
        //        Rs += (transform.position - dc.transform.position);

        //    Vector3 Ra = new Vector3();
        //    Vector3 LCM = new Vector3();
        //    foreach (SheepController sc in sheepNeighbours)
        //    {
        //        // repulsion from interacting neighbours
        //        if ((transform.position - sc.transform.position).magnitude < GM.SheepParametersStrombom.r_a)
        //            Ra += (transform.position - sc.transform.position).normalized;
        //        LCM += sc.transform.position;
        //    }
        //    LCM += transform.position;
        //    LCM = LCM / (sheepNeighbours.Count() + 1);

        //    // attraction towards LCM
        //    Vector3 Ci = new Vector3();
        //    Ci += LCM - transform.position;

        //    // noise
        //    float eps = Random.Range(-Mathf.PI * GM.SheepParametersStrombom.e, Mathf.PI * GM.SheepParametersStrombom.e);
        //    desiredThetaVector += GM.SheepParametersStrombom.h * transform.forward +
        //      GM.SheepParametersStrombom.c * Ci.normalized +
        //      GM.SheepParametersStrombom.rho_a * Ra.normalized +
        //      GM.SheepParametersStrombom.rho_s * Rs.normalized;

        //    Vector3 desiredThetaVector2 = GM.SheepParametersStrombom.h * transform.forward +
        //      GM.SheepParametersStrombom.c * Ci.normalized +
        //      GM.SheepParametersStrombom.rho_a * (fuzzySheepRepulsion * Ra).normalized +
        //      GM.SheepParametersStrombom.rho_s * (fuzzyDogRepulsion * Rs).normalized;

        //    // repulsion from fences and trees
        //    float r_f2 = GM.SheepParametersStrombom.r_f * GM.SheepParametersStrombom.r_f;
        //    foreach (Collider fenceCollider in GM.fenceColliders)
        //    {
        //        Vector3 closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
        //        if ((transform.position - closestPoint).sqrMagnitude < r_f2)
        //        {
        //            Vector3 e_ij = closestPoint - transform.position;
        //            float d_ij = e_ij.magnitude;

        //            float f_ij = Mathf.Min(.0f, (d_ij - GM.SheepParametersStrombom.r_f) / GM.SheepParametersStrombom.r_f);
        //            desiredThetaVector += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;
        //            desiredThetaVector2 += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;
        //            // if walking transition to idle mode the closer to the fence the more likely
        //            if (sheepState == Enums.SheepState.walking)
        //                if (Random.Range(.0f, 1.0f) < 1f - (d_ij / GM.SheepParametersStrombom.r_f))
        //                    sheepState = Enums.SheepState.idle;
        //        }
        //    }

        //    // extract desired heading
        //    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
        //    float desiredTheta2 = (Mathf.Atan2(desiredThetaVector2.z, desiredThetaVector2.x) + fuzzyNoise * eps) * Mathf.Rad2Deg;
        //if (this.id == 1)
        //{
        //    Debug.Log("Fuzzy vals: " + fuzzyDogRepulsion + ", " + fuzzySheepRepulsion + ", " + fuzzyNoise);
        //    Debug.Log("Fuzzy comp: " + Rs + " " + fuzzyDogRepulsion * Rs + " " + Ra + ", " + fuzzySheepRepulsion * Ra + ", " + eps + " " + fuzzyNoise * eps);
        //    Debug.Log("Fuzzy theta: " + desiredThetaVector + " " + desiredThetaVector2 + ", " + desiredTheta + " " + desiredTheta2);
        //}
        //}

        //this.desiredTheta;
        //this.sheepState;

        SetSpeed();
    }

    void StrombomUpdate()
    {


        Vector3 desiredThetaVector = new Vector3();
        var dogs = GM.dogList.Where(dog => (dog.transform.position - transform.position).sqrMagnitude < GM.SheepParametersStrombom.r_s * GM.SheepParametersStrombom.r_s);
        if (GM.SheepParametersStrombom.occlusion)
            dogs = dogs.Where(dog => IsVisible(dog, GM.SheepParametersStrombom.blindAngle));
        dogs = dogs.OrderBy(d => d, new ByDistanceFrom(transform.position))
            .Take(GM.SheepParametersStrombom.n);
        var sheepNeighbours = GM.sheepList.Where(sheepNeighbour => !sheepNeighbour.dead && sheepNeighbour.id != id);
        if (GM.SheepParametersStrombom.occlusion)
            sheepNeighbours = sheepNeighbours.Where(sheepNeighbour => sheepNeighbour.IsVisible(sheepNeighbour, GM.SheepParametersStrombom.blindAngle));
#if false // call transform.position
    sheepNeighbours = sheepNeighbours
            .OrderBy(d => d, new ByDistanceFrom(transform.position))
            .Take(GM.SheepParametersStrombom.n);
#else // use cached position
        sheepNeighbours = sheepNeighbours
                .OrderBy(d => d, new ByDistanceFrom(this))
                .Take(GM.SheepParametersStrombom.n);
#endif
        Vector3[] SheepPos = SheepUtils.GetSheepPositions(sheepNeighbours);
        Vector3[] DogPos = SheepUtils.GetDogPositions(dogs);
        float[] sheepDist = SheepUtils.CalculateDistances(this.position, SheepPos);
        float[] dogDist = SheepUtils.CalculateDistances(this.position, DogPos);

        float[] fuzzy_values = fuzzyLogic.fuzzyfy(sheepDist, dogDist);
        // ker je na range 0 do 1, damo *2, da je utez med 0 in 2
        float fuzzyNoise = fuzzy_values[0];
        float fuzzyDogRepulsion = fuzzy_values[1];
        float fuzzySheepRepulsion = fuzzy_values[2];


        if (dogs.Count() == 0)
        {
            if (Random.Range(.0f, 1.0f) < .05f)
                sheepState = Enums.SheepState.walking;
            else
                sheepState = Enums.SheepState.idle;
        }
        else
        {
            // are there any dogs very close
            if (dogs.Where(dog => (dog.transform.position - transform.position).magnitude < GM.SheepParametersStrombom.r_sS).Count() > 0)
                sheepState = Enums.SheepState.running;
            else
                sheepState = Enums.SheepState.walking;
        }

        if (sheepState != Enums.SheepState.idle)
        {
            // repulsion directly from shepherds
            Vector3 Rs = new Vector3();
            foreach (DogController dc in dogs)
                Rs += (transform.position - dc.transform.position);

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
            LCM = LCM / (sheepNeighbours.Count() + 1);

            // attraction towards LCM
            Vector3 Ci = new Vector3();
            Ci += LCM - transform.position;

            // noise
            float eps = Random.Range(-Mathf.PI * GM.SheepParametersStrombom.e, Mathf.PI * GM.SheepParametersStrombom.e);
            desiredThetaVector += GM.SheepParametersStrombom.h * transform.forward +
              GM.SheepParametersStrombom.c * Ci.normalized +
              GM.SheepParametersStrombom.rho_a * Ra.normalized +
              GM.SheepParametersStrombom.rho_s * Rs.normalized;

            Vector3 desiredThetaVector2 = GM.SheepParametersStrombom.h * transform.forward +
              GM.SheepParametersStrombom.c * Ci.normalized +
              GM.SheepParametersStrombom.rho_a * (fuzzySheepRepulsion * Ra).normalized +
              GM.SheepParametersStrombom.rho_s * (fuzzyDogRepulsion * Rs).normalized;

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
                    desiredThetaVector2 += GM.SheepParametersStrombom.rho_f * f_ij * e_ij.normalized;
                    // if walking transition to idle mode the closer to the fence the more likely
                    if (sheepState == Enums.SheepState.walking)
                        if (Random.Range(.0f, 1.0f) < 1f - (d_ij / GM.SheepParametersStrombom.r_f))
                            sheepState = Enums.SheepState.idle;
                }
            }

            // extract desired heading
            desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
            float desiredTheta2 = (Mathf.Atan2(desiredThetaVector2.z, desiredThetaVector2.x) + fuzzyNoise * eps) * Mathf.Rad2Deg;
            //if (this.id == 1)
            //{
            //    Debug.Log("Fuzzy vals: " + fuzzyDogRepulsion + ", " + fuzzySheepRepulsion + ", " + fuzzyNoise);
            //    Debug.Log("Fuzzy comp: " + Rs + " " + fuzzyDogRepulsion * Rs + " " + Ra + ", " + fuzzySheepRepulsion * Ra + ", " + eps + " " + fuzzyNoise * eps);
            //    Debug.Log("Fuzzy theta: " + desiredThetaVector + " " + desiredThetaVector2 + ", " + desiredTheta + " " + desiredTheta2);
            //}
        }

        SetSpeed();
    }
}