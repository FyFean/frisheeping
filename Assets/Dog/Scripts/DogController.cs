using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class DogController : MonoBehaviour
{
  public int id;

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
  private float desiredV = .0f;
  private float v;

  private float theta;
  private float desiredTheta = .0f;

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
    if (GM.StrombomDogsPlus)
      BehaviourLogicStrombomPlus();
    else if (GM.StrombomDogs)
      BehaviourLogicStrombom();
    else
      Controls();
    if (GM.useFixedTimestep) {
      DogMovement();
    }
  }

  void FixedUpdate()
  {
    if (!GM.useFixedTimestep) {
      DogMovement();
    }
  }

  void Controls()
  {
    float timestep;
    if (GM.useFixedTimestep) {
      timestep = GM.fixedTimestep;
    } else {
      timestep = Time.deltaTime;
    }
    if (Input.GetKey(KeyCode.W))
    {
      desiredV += GM.dogMaxSpeedChange * timestep;
    }
    else if (Input.GetKey(KeyCode.S))
    {
      desiredV -= GM.dogMaxSpeedChange * timestep;
    }
    else
    {
      desiredV = Mathf.MoveTowards(desiredV, 0, GM.dogMaxSpeedChange / 10f);
    }

    if (Input.GetKey(KeyCode.A))
    {
      desiredTheta += GM.dogMaxTurn * timestep;
    }
    else if (Input.GetKey(KeyCode.D))
    {
      desiredTheta -= GM.dogMaxTurn * timestep;
    }
    else
    {
      desiredTheta = theta + Mathf.MoveTowardsAngle((desiredTheta - theta), 0, GM.dogMaxTurn / 10f);
      // ensure angle remains in [-180,180)
      desiredTheta = (desiredTheta + 180f) % 360f - 180f;
    }
  }

  private bool IsVisible(SheepController sc, float blindAngle)
  {
#if true // experimental: test occlusion
    Vector3 Cm = GetComponent<Rigidbody>().worldCenterOfMass;
    Vector3 toCm = sc.GetComponent<Rigidbody>().worldCenterOfMass - Cm;
    bool hit = Physics.Raycast(Cm + .5f * toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
    if (GM.DogsParametersStrombom.dynamicBlindAngle) {
      blindAngle = blindAngle + (GM.DogsParametersStrombom.runningBlindAngle - blindAngle) * (this.v / GM.dogRunningSpeed);
    }
    Vector3 toSc = sc.transform.position - transform.position;
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }

  private bool IsBehindDog(SheepController sc, Vector3 cm, Vector3 dog)
  {
    float d_dog = (sc.transform.position - dog).magnitude;
    float d_cm = (sc.transform.position - cm).magnitude;
    float d_dog_cm = (dog - cm).magnitude;
    return Mathf.Pow(d_dog, 2) + Mathf.Pow(d_dog_cm, 2) < Mathf.Pow(d_cm, 2);
  }

  void BehaviourLogicStrombom()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;

    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    // get only live sheep
    var sheep = GM.sheepList.Where(sc => !sc.dead);
    if (GM.DogsParametersStrombom.local)
    { // localized perception
      if (GM.DogsParametersStrombom.occlusion)
        sheep = sheep.Where(sc => IsVisible(sc, GM.DogsParametersStrombom.blindAngle));

#if false // experimental: exlude visually occludded sheep
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
#endif
#if true // take into account cognitive limits track max ns nearest neighbours
      sheep = sheep
        .OrderBy(d => d, new ByDistanceFrom(transform.position))
        .Take(GM.DogsParametersStrombom.ns); 
#endif
    }

    if (sheep.Count() > 0)
    {
      // compute CM of sheep
      Vector3 CM = new Vector3();
      foreach (SheepController sc in sheep)
        CM += sc.transform.position;
      if (sheep.Count() > 0)
        CM /= (float)sheep.Count();

      // draw CM
      Vector3 X = new Vector3(1, 0, 0);
      Vector3 Z = new Vector3(0, 0, 1);
      Color color = new Color(0f, 0f, 0f, 1f);
      Debug.DrawRay(CM - X, 2 * X, color);
      Debug.DrawRay(CM - Z, 2 * Z, color);

      // find distance of sheep that is nearest to the dog & distance of sheep furthest from CM
      float md_ds = Mathf.Infinity;
      SheepController sheep_c = null; // sheep furthest from CM
      float Md_sC = 0;

      foreach (SheepController sc in sheep)
      {
        // distance from CM
        float d_sC = (CM - sc.transform.position).magnitude;
        if (d_sC > Md_sC)
        {
          Md_sC = d_sC;
          sheep_c = sc;
        }

        // distance from dog
        float d_ds = (sc.transform.position - transform.position).magnitude;
        md_ds = Mathf.Min(md_ds, d_ds);
      }

      float ro = 0; // mean nnd
      if (GM.StrombomSheep)
        ro = GM.SheepParametersStrombom.r_a;
      else
        ro = GM.SheepParametersGinelli.r_0;

#if false // aproximate interaction distance through nearest neigbour distance
      foreach (SheepController sheep in sheepList)
      {
        float nn = Mathf.Infinity;
        foreach (SheepController sc in sheepList)
        {
          if (sc.id == sheep.id) continue;
          nn = Mathf.Min(nn, (sheep.transform.position - sc.transform.position).magnitude);
        }
        ro += nn;
      }
      ro /= sheepList.Count;
#endif

      float r_s = GM.DogsParametersStrombom.r_s * ro; // compute true stopping distance
      float r_w = GM.DogsParametersStrombom.r_w * ro; // compute true walking distance
      float r_r = GM.DogsParametersStrombom.r_r * ro; // compute true running distance

      // if too close to any sheep stop and wait
      if (md_ds < r_s)
      {
        dogState = Enums.DogState.idle;
        desiredV = .0f;
      }
      // if close to any sheep start walking
      else if (md_ds < r_w)
      {
        dogState = Enums.DogState.walking;
        desiredV = GM.dogWalkingSpeed;
      }
      else if (md_ds > r_r)
      {
        // default run in current direction
        dogState = Enums.DogState.running;
        desiredV = GM.dogRunningSpeed;
      }

      // aproximate radius of a circle
      float f_N = ro * Mathf.Pow(sheep.Count(), 2f / 3f);
      // draw aprox herd size
      Debug.DrawCircle(CM, f_N, new Color(1f, 0f, 0f, 1f));

#if true
      foreach (SheepController sc in sheep)
        Debug.DrawCircle(sc.transform.position, .5f, new Color(1f, 0f, 0f, 1f));
#endif

      // if all agents in a single compact group, collect them
      if (Md_sC < f_N)
      {
        BarnController barn = FindObjectOfType<BarnController>();

        // compute position so that the GCM is on a line between the dog and the target
        Vector3 Pd = CM + (CM - barn.transform.position).normalized * ro * Mathf.Sqrt(sheep.Count()); // Mathf.Min(ro * Mathf.Sqrt(sheep.Count), Md_sC);
        desiredThetaVector = Pd - transform.position;
        if (desiredThetaVector.magnitude > r_w)
          desiredV = GM.dogRunningSpeed;

        color = new Color(0f, 1f, 0f, 1f);
        Debug.DrawRay(Pd - X - Z, 2 * X, color);
        Debug.DrawRay(Pd + X - Z, 2 * Z, color);
        Debug.DrawRay(Pd + X + Z, -2 * X, color);
        Debug.DrawRay(Pd - X + Z, -2 * Z, color);
      }
      else
      {
        // compute position so that the sheep most distant from the GCM is on a line between the dog and the GCM
        Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
        // move in an arc around the herd??
        desiredThetaVector = Pc - transform.position;

        color = new Color(1f, .5f, 0f, 1f);
        Debug.DrawRay(Pc - X - Z, 2 * X, color);
        Debug.DrawRay(Pc + X - Z, 2 * Z, color);
        Debug.DrawRay(Pc + X + Z, -2 * X, color);
        Debug.DrawRay(Pc - X + Z, -2 * Z, color);
      }
    }
    else
    {
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
    /* end of behaviour logic */
  }

  void BehaviourLogicStrombomPlus()
  {
    float timestep;
    if (GM.useFixedTimestep) {
      timestep = GM.fixedTimestep;
    } else {
      timestep = Time.deltaTime;
    }
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;

    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    // get only live sheep
    List<SheepController> sheep = new List<SheepController>(GM.sheepList).Where(sc => !sc.dead).ToList();
    if (GM.DogsParametersStrombom.local)
    { // localized perception
      if (GM.DogsParametersStrombom.occlusion)
        sheep = sheep.Where(sc => IsVisible(sc, GM.DogsParametersStrombom.blindAngle)).ToList();

#if false // experimental: exlude visually occludded sheep
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
#endif
#if true // take into account cognitive limits track max ns nearest neighbours
      sheep.Sort(new ByDistanceFrom(transform.position));
      sheep = sheep.GetRange(0, Mathf.Min(GM.DogsParametersStrombom.ns, sheep.Count));
#endif
    }

    if (sheep.Count > 0)
    {
      // compute CM of sheep
      Vector3 CM = new Vector3();
      foreach (SheepController sc in sheep)
        CM += sc.transform.position;
      if (sheep.Count > 0)
        CM /= (float)sheep.Count;


      // check if dog is closer to CM than average sheep, if true the herd is split
      /*
      float totalDistFromCM = 0;
      foreach (SheepController sc in sheep)
        totalDistFromCM += (sc.transform.position - CM).magnitude;
      float avgDistFromCM = 0;
      if (sheep.Count > 0)
        avgDistFromCM = totalDistFromCM / (float)sheep.Count;
      float dogDistFromCM = (transform.position - CM).magnitude;
      if (avgDistFromCM > dogDistFromCM) // dog is between two or more herds => ignore one side
        sheep = sheep.Where(s => !IsBehindDog(s, CM, transform.position)).ToList();
      */



      // draw CM
      Vector3 X = new Vector3(1, 0, 0);
      Vector3 Z = new Vector3(0, 0, 1);
      Color color = new Color(0f, 0f, 0f, 1f);
      Debug.DrawRay(CM - X, 2 * X, color);
      Debug.DrawRay(CM - Z, 2 * Z, color);

      // find distance of sheep that is nearest to the dog & distance of sheep furthest from CM
      float md_ds = Mathf.Infinity;
      SheepController sheep_c = null; // sheep furthest from CM
                                      //float Md_sC = 0;
      float Md_sC = 0.01f;

      float max_priority = -Mathf.Infinity;
      foreach (SheepController sc in sheep)
      {
        // distance from CM
        float d_sC = (CM - sc.transform.position).magnitude;

        // modification: prioritize sheep closer to dog

        Vector3 vectorToSheep = (sc.transform.position - transform.position);
        float thetaToSheep = Mathf.Atan2(vectorToSheep.z, vectorToSheep.x) * Mathf.Rad2Deg;
        float angleDelta = ((thetaToSheep - theta) + 180f) % 360f - 180f;

        float d_dog = (transform.position - sc.transform.position).magnitude;
        //float priority = d_sC - Mathf.Sqrt(d_dog);
        //float priority = d_sC - d_dog;
        

        // prioritize the sheep currently in front of the dog

        // linear priority scaling based on angle, 1 in front ... 0.5 directly behind
        //float priority = d_sC * (1f - Mathf.Abs(angleDelta/180f) * 0.5f);
        // quadratic priority scaling based on angle, 1 in front ... 0 directly behind
        float priority = d_sC * Mathf.Pow(1f - Mathf.Abs(angleDelta/180f) * 1f, 2);

        if (priority > max_priority) {
          max_priority = priority;
          Md_sC = d_sC;
          sheep_c = sc;
        }



        //if (d_sC > Md_sC)
        //if (d_sC > Md_sC && d_sC / Md_sC > 1.5) // try to reduce target swapping
        //{
        //  Md_sC = d_sC;
        //  sheep_c = sc;
        //}

        // distance from dog
        float d_ds = (sc.transform.position - transform.position).magnitude;
        md_ds = Mathf.Min(md_ds, d_ds);
      }

      float ro = 0; // mean nnd
      if (GM.StrombomSheep)
        ro = GM.SheepParametersStrombom.r_a;
      else
        ro = GM.SheepParametersGinelli.r_0;

#if false // aproximate interaction distance through nearest neigbour distance
      foreach (SheepController sheep in sheepList)
      {
        float nn = Mathf.Infinity;
        foreach (SheepController sc in sheepList)
        {
          if (sc.id == sheep.id) continue;
          nn = Mathf.Min(nn, (sheep.transform.position - sc.transform.position).magnitude);
        }
        ro += nn;
      }
      ro /= sheepList.Count;
#endif

      float r_s = GM.DogsParametersStrombom.r_s * ro; // compute true stopping distance
      float r_w = GM.DogsParametersStrombom.r_w * ro; // compute true walking distance
      float r_r = GM.DogsParametersStrombom.r_r * ro; // compute true running distance

      // if too close to any sheep stop and wait
      if (md_ds < r_s)
      {
        dogState = Enums.DogState.idle;
        desiredV = .0f;
      }
      // if close to any sheep start walking
      else if (md_ds < (r_s + r_w) / 2)
      {
        dogState = Enums.DogState.walking;
        desiredV = GM.dogWalkingSpeed;
      }
      //else if (md_ds < r_w)
      else if (md_ds < r_r)
      {
        dogState = Enums.DogState.running;
        desiredV = GM.dogRunningSpeed;
      }
      else if (md_ds > r_r)
      {
        // default run in current direction
        dogState = Enums.DogState.running;
        desiredV = GM.dogRunningSpeed;
      }

      // aproximate radius of a circle
      float f_N = ro * Mathf.Pow(sheep.Count, 2f / 3f);
      // draw aprox herd size
      Debug.DrawCircle(CM, f_N, new Color(1f, 0f, 0f, 1f));

#if true
      foreach (SheepController sc in sheep)
        Debug.DrawCircle(sc.transform.position, .5f, new Color(1f, 0f, 0f, 1f));
#endif
      bool driving = false;
      // if all agents in a single compact group, collect them
      //if (Md_sC < f_N)
      // modified: if we have multiple dogs, one is always in driving mode
      if (Md_sC < f_N || (GM.dogList.Count() > 1 && id == 0))
      {
        BarnController barn = FindObjectOfType<BarnController>();

        // compute position so that the GCM is on a line between the dog and the target
        Vector3 Pd = CM + (CM - barn.transform.position).normalized * ro * Mathf.Sqrt(sheep.Count); // Mathf.Min(ro * Mathf.Sqrt(sheep.Count), Md_sC);
        desiredThetaVector = Pd - transform.position;
        if (desiredThetaVector.magnitude > r_w)
          desiredV = GM.dogRunningSpeed;

        color = new Color(0f, 1f, 0f, 1f);
        Debug.DrawRay(Pd - X - Z, 2 * X, color);
        Debug.DrawRay(Pd + X - Z, 2 * Z, color);
        Debug.DrawRay(Pd + X + Z, -2 * X, color);
        Debug.DrawRay(Pd - X + Z, -2 * Z, color);

        driving = true;
      }
      else
      {
        // compute position so that the sheep most distant from the GCM is on a line between the dog and the GCM
        Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
        // move in an arc around the herd??
        desiredThetaVector = Pc - transform.position;

        color = new Color(1f, .5f, 0f, 1f);
        Debug.DrawRay(Pc - X - Z, 2 * X, color);
        Debug.DrawRay(Pc + X - Z, 2 * Z, color);
        Debug.DrawRay(Pc + X + Z, -2 * X, color);
        Debug.DrawRay(Pc - X + Z, -2 * Z, color);

      }

      if (GM.dogRepulsion && GM.dogList.Count() > 1)
      {
        float repulsionDistance = (id + 3) * 5 / 3f;
        List<DogController> otherDogs = new List<DogController>(GM.dogList).Where(d => d != this).ToList();
        Vector3 repulsionVector = new Vector3(0f, 0f, 0f);
        foreach (DogController d in otherDogs)
        {
          if ((transform.position - d.transform.position).magnitude < repulsionDistance)
          {
            repulsionVector += (transform.position - d.transform.position);
          }
        }
        desiredThetaVector += repulsionVector;
        Debug.DrawCircle(transform.position, repulsionDistance, new Color(0f, 1f, 1f, 1f));
        Debug.DrawLine(transform.position, transform.position + repulsionVector);
      }     


      // arc movement
      // direct line to sheep furthest from CM
      color = Color.cyan;
      if (driving) {
        Debug.DrawRay(transform.position, desiredThetaVector, new Color(0f, 1f, 0f, 1f));
      } else {
        Debug.DrawRay(transform.position, desiredThetaVector, new Color(1f, .5f, 0f, 1f));
      }
      // arc around CM
      Debug.DrawCircle(CM, (transform.position - CM).magnitude, Color.blue, false);

      //Vector3 cmVector = transform.position + (transform.position - CM);
      Vector3 cmVector = CM - transform.position;
      Debug.DrawRay(transform.position, cmVector, Color.red);
      float cmTheta = (Mathf.Atan2(cmVector.z, cmVector.x) + eps) * Mathf.Rad2Deg;
      desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;

      float delta = cmTheta - desiredTheta;
      delta = (delta + 180f) % 360f - 180f;


      if (Mathf.Abs(delta) < 90) // only use arc if furthest sheep is closer to CM than dog
      {
        float arcAngle = 85f; // maintain distance from CM
                              // TODO include (ratio with repulsion) distance in equation?
        if (driving && desiredThetaVector.magnitude > cmVector.magnitude)
        {
          // driving position is on other side of the herd, go around
        }
        else
        {
          arcAngle = Mathf.Min(3 * Mathf.Abs(delta), arcAngle); // get closer if angle between furthest sheep and CM is small
        }

        // calculate new desired angle
        float arcTheta = 0;
        if (delta < 0) arcTheta = (cmTheta + arcAngle + 180f) % 360f - 180f;
        else arcTheta = (cmTheta - arcAngle + 180f) % 360f - 180f;
        float arcThetaRad = arcTheta * Mathf.Deg2Rad;

        Vector3 arcVector = (new Vector3(Mathf.Cos(arcThetaRad), 0, Mathf.Sin(arcThetaRad)) * 10);


        // correct angle to avoid fence
        float angleCorrectionStep = 10; // degrees
        angleCorrectionStep = (((desiredTheta - arcTheta + 180f) % 360f) - 180f) > 0 ? angleCorrectionStep : -angleCorrectionStep;

        RaycastHit hit;
        for (int i = 0; i < (180f / Mathf.Abs(angleCorrectionStep)); i++)
        {
          if (Physics.Raycast(transform.position, arcVector, out hit, 10f))
          {
            //Debug.DrawRay(transform.position, arcVector * hit.distance, Color.yellow);
            // close to fence, adjust angle
            arcTheta = arcTheta + angleCorrectionStep;
            arcThetaRad = arcTheta * Mathf.Deg2Rad;
            arcVector = (new Vector3(Mathf.Cos(arcThetaRad), 0, Mathf.Sin(arcThetaRad)) * 10);
          }
          else
          {
            Debug.DrawRay(transform.position, arcVector, Color.white);
            desiredThetaVector = arcVector;
            break;
          }
        }
        

        
        // make dog always run when collecting sheep
        /*if (!driving)
        {
          dogState = Enums.DogState.running;
          desiredV = GM.dogRunningSpeed;
        }*/

      }

      List<ConvexHull.Point> points = new List<ConvexHull.Point>();
      foreach (SheepController sc in sheep) {
        points.Add(new ConvexHull.Point(sc.position.x, sc.position.z));
      }
      List<ConvexHull.Point> hull = ConvexHull.convexHull(points);
      ConvexHull.Point prev = hull.Last();
      foreach (ConvexHull.Point p in hull) {
        Debug.DrawRay(new Vector3(prev.x, 0, prev.y), new Vector3(p.x - prev.x, 0, p.y - prev.y), new Color(1f, 0f, 1f, 0.4f));
        prev = p;
      }
      List<ConvexHull.Point> expandedHull = new List<ConvexHull.Point>();
      if (hull.Count > 1) {
        for (int i = 0; i < hull.Count(); i++) {
          prev = i > 0 ? hull[i-1] : hull.Last();
          ConvexHull.Point next = i < hull.Count - 1 ? hull[i+1] : hull[0];
          float prevAngle = (Mathf.Atan2(prev.y - hull[i].y, prev.x - hull[i].x) + eps) * Mathf.Rad2Deg;
          float prevPlus90 = (prevAngle + 90f + 180f) % 360f - 180f;
          float nextAngle = (Mathf.Atan2(next.y - hull[i].y, next.x - hull[i].x) + eps) * Mathf.Rad2Deg;
          float nextMinus90 = (nextAngle - 90f + 180f) % 360f - 180f;
          //float delta12 = (nextMinus90 - prevPlus90 + 180f) % 360f - 180f;
          float delta12 = (nextMinus90 - prevPlus90 + 720f) % 360f;
          int nSteps = (int)(delta12/30f) + 1;
          //Debug.Log(nSteps);
          Vector3 currentPointVector = new Vector3(hull[i].x, 0, hull[i].y);
          for(int j = 0; j <= nSteps; j++) {
            float angle = (prevPlus90 + j * (delta12/nSteps)) * Mathf.Deg2Rad;
            Vector3 vec = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 5f + currentPointVector;
            ConvexHull.Point p = new ConvexHull.Point(vec.x, vec.z);
            expandedHull.Add(p);
            
          }
          /*
          float prevPlus90Rad = prevPlus90 * Mathf.Deg2Rad;
          float nextMinus90Rad = nextMinus90 * Mathf.Deg2Rad;
          Vector3 prevPlus90Vector = new Vector3(Mathf.Cos(prevPlus90Rad), 0, Mathf.Sin(prevPlus90Rad)) * 5f;
          Vector3 nextMinus90Vector = new Vector3(Mathf.Cos(nextMinus90Rad), 0, Mathf.Sin(nextMinus90Rad)) * 5f;
          Vector3 v1 = new Vector3(hull[i].x, 0, hull[i].y) + prevPlus90Vector;
          Vector3 v2 = new Vector3(hull[i].x, 0, hull[i].y) + nextMinus90Vector;
          ConvexHull.Point p1 = new ConvexHull.Point(v1.x, v1.z);
          ConvexHull.Point p2 = new ConvexHull.Point(v2.x, v2.z);
          expandedHull.Add(p1);
          expandedHull.Add(p2);
          */
        }
      } else {
        Vector3 currentPointVector = new Vector3(hull[0].x, 0, hull[0].y);
        for (int i = 0; i < 12; i++) {
          float angle = i * 30 * Mathf.Deg2Rad;
          Vector3 vec = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 5f + currentPointVector;
          ConvexHull.Point p = new ConvexHull.Point(vec.x, vec.z);
          expandedHull.Add(p);
        }
      }
      
      prev = expandedHull.Last();
      foreach (ConvexHull.Point p in expandedHull) {
        Debug.DrawRay(new Vector3(prev.x, 0, prev.y), new Vector3(p.x - prev.x, 0, p.y - prev.y), new Color(1f, 0f, 1f, 0.7f));
        prev = p;
      }
        //Debug.DrawCircle(sc.transform.position, .5f, new Color(1f, 0f, 0f, 1f));


    }
    else // no visible sheep
    {
      //dogState = Enums.DogState.idle;
      //desiredV = .0f;
      // turn around after losing vision of sheep instead of standing still
      dogState = Enums.DogState.walking;
      desiredV = GM.dogWalkingSpeed;
      desiredTheta = (desiredTheta - GM.dogMaxTurn * timestep + 180f) % 360f - 180f;
      return;
    }

    
    if (GM.DogsParametersStrombom.occlusion) {
          float blindAngle = GM.DogsParametersStrombom.blindAngle;
          if (GM.DogsParametersStrombom.dynamicBlindAngle) {
            blindAngle = blindAngle + (GM.DogsParametersStrombom.runningBlindAngle - blindAngle) * (this.v / GM.dogRunningSpeed);
          }
          float blindAngle1 = ((theta + blindAngle/2 + 360f) % 360f - 180f) * Mathf.Deg2Rad;
          Vector3 blindVector1 = new Vector3(Mathf.Cos(blindAngle1), 0, Mathf.Sin(blindAngle1));
          Debug.DrawRay(transform.position, blindVector1 * 100f, new Color(0.8f, 0.8f, 0.8f, 0.2f));
          float blindAngle2 = ((theta - blindAngle/2 + 360f) % 360f - 180f) * Mathf.Deg2Rad;
          Vector3 blindVector2 = new Vector3(Mathf.Cos(blindAngle2), 0, Mathf.Sin(blindAngle2));
          Debug.DrawRay(transform.position, blindVector2 * 100f, new Color(0.8f, 0.8f, 0.8f, 0.2f));
        }

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;
    /* end of behaviour logic */
  }


  void DogMovement()
  {
    float timestep;
    if (GM.useFixedTimestep) {
      timestep = GM.fixedTimestep;
    } else {
      timestep = Time.deltaTime;
    }
    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, GM.dogMaxTurn * timestep);
    // ensure angle remains in [-180,180)
    theta = (theta + 180f) % 360f - 180f;
    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, GM.dogMaxSpeedChange * timestep);
    // ensure speed remains in [minSpeed, maxSpeed]
    v = Mathf.Clamp(v, GM.dogMinSpeed, GM.dogMaxSpeed);

    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;
    // update position
    Vector3 newPosition = transform.position + (timestep * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    // draw dogRepulsion radius
    if (GM.StrombomSheep)
    {
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_s, new Color(1f, 1f, 0f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_sS, new Color(1f, 1f, 0f, 1f));
    }
    else
    {
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_s, new Color(1f, 1f, 0f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_sS, new Color(1f, 1f, 0f, 1f));
    }

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
    anim.SetBool("IsRunning", v > 6);
    anim.SetBool("Reverse", v < 0);
    anim.SetBool("IsWalking", (v > 0 && v <= 6) ||
                               turningLeft ||
                               turningRight);

  }
}