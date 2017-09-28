using UnityEngine;

public class DogController : MonoBehaviour
{
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
    Controls();

    //wwwwBehaviourLogic();

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
    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */
    // heading
    Vector3 desiredThetaVector = transform.forward;
    desiredTheta = Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.y);
    // speed
    desiredV = 5.0f;
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