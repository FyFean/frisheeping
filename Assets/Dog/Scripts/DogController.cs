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
  private float desiredV;
  private float v = .0f;
  private Vector3 desiredTheta;
  private Vector3 newHeading;
  private float maxTurn = .03f;
  private float maxSpeedChange = 1.0f;

  // animation bools
  private bool turningLeft = false;
  private bool turningRight = false;

  // Use this for initialization
  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // init speed and direction
    v = 5.0f;
    desiredV = v;
    desiredTheta = transform.forward;
  }

  // Update is called once per frame
  void FixedUpdate()
  {
    DogMovement();
  }

  void DogMovement()
  {
    /* IMPLEMENT DOG LOGIC HERE */
    // direction
    desiredTheta = transform.forward;

    // speed
    desiredV = 5.0f;

    // update heading
    newHeading = Vector3.RotateTowards(transform.forward, desiredTheta, maxTurn, .0f);
    newHeading.y = .0f;

    // update speed
    float deltaV = desiredV - v;
    v += Mathf.Min(maxSpeedChange, Mathf.Abs(deltaV)) * Mathf.Sign(deltaV);
    v = Mathf.Clamp(v, minSpeed, maxSpeed);

    Vector3 newPosition = transform.position + (Time.deltaTime * v * newHeading);
    newPosition.y = .0f;

    transform.position = newPosition;
    transform.forward = newHeading;

    if (v == .0f)
    {
      turningLeft = false;
      turningRight = false;
    }

    // Animation Controller
    anim.SetBool("IsRunning", v > 8);
    anim.SetBool("Reverse", v < 0);
    anim.SetBool("IsWalking", (v > 0 && v <= 8) ||
                               turningLeft ||
                               turningRight);
  }
}