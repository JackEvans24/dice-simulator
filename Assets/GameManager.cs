using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform mainCamera;

    [Header("Pre-Throw")]
    public Rigidbody diePrefab;
    public Transform diceStartPos;
    public Vector3 diceStartRotation;
    public Vector3 dieRotation;

    [Header("Throw")]
    public Vector3 throwVelocity;

    [Header("Post-Throw")]
    public float minMagnitude = 0.05f;
    public float settleWaitTime;
    private bool waitingToSettle;
    private float currentSettleWaitTime;
    public Vector3 cameraOffset;
    public Vector3 cameraRotation;
    public float cameraMoveDuration;
    public LeanTweenType cameraMoveCurve;
    private bool cameraMoved;

    private Rigidbody currentDie;

    private bool canThrow;
    private bool throwing;
    private bool thrown;
    private bool canReset;

    private Vector3 originalCameraPos;
    private Vector3 originalCameraRotation;

    void Start()
    {
        originalCameraPos = mainCamera.position;
        originalCameraRotation = mainCamera.rotation.eulerAngles;

        currentDie = GameObject.Instantiate(diePrefab, diceStartPos.position, Quaternion.Euler(diceStartRotation));

        canThrow = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
            Application.Quit();

        if (thrown)
            CheckDieIsAtRest();
        else if (!throwing && !thrown)
        {
            if (canThrow && Input.GetButtonDown("Jump"))
            {
                throwing = true;
                canThrow = false;
                return;
            }
        }

        if (waitingToSettle && currentSettleWaitTime <= settleWaitTime)
            currentSettleWaitTime += Time.deltaTime;

        if (!cameraMoved && currentSettleWaitTime > settleWaitTime)
            MoveCamera();

        if (canReset && Input.GetButtonDown("Jump"))
            Reset();
    }

    void FixedUpdate()
    {
        if (!throwing)
        {
            currentDie.angularVelocity = dieRotation;
        }
        else if (!thrown)
        {
            Throw();
        }
    }

    private void Throw()
    {
        if (thrown)
            return;

        thrown = true;

        currentDie.constraints = RigidbodyConstraints.None;
        currentDie.AddForce(throwVelocity, ForceMode.VelocityChange);
    }

    private void CheckDieIsAtRest()
    {
        waitingToSettle = currentDie.velocity.magnitude < minMagnitude;

        if (!waitingToSettle)
            currentSettleWaitTime = 0;
    }

    private void MoveCamera()
    {
        Tween(mainCamera.gameObject, currentDie.position + cameraOffset, cameraRotation, cameraMoveDuration)
            .setOnComplete(() => canReset = true);

        cameraMoved = true;
    }

    private LTDescr Tween(GameObject obj, Vector3 position, Vector3 rotation, float duration)
    {
        LeanTween.moveY(obj, position.y, duration).setEase(cameraMoveCurve);
        LeanTween.moveX(obj, position.x, duration).setEase(cameraMoveCurve);
        LeanTween.moveZ(obj, position.z, duration).setEase(cameraMoveCurve);
        return LeanTween
            .rotate(obj, rotation, cameraMoveDuration)
            .setEase(cameraMoveCurve);
    }

    private void Reset()
    {
        if (!canReset)
            return;

        canReset = false;

        Tween(mainCamera.gameObject, originalCameraPos, originalCameraRotation, cameraMoveDuration)
            .setOnComplete(() => canThrow = true);

        waitingToSettle = false;
        currentSettleWaitTime = 0;

        Tween(currentDie.gameObject, diceStartPos.position, diceStartRotation, cameraMoveDuration)
            .setOnComplete(() => currentDie.constraints = RigidbodyConstraints.FreezePosition);

        throwing = thrown = cameraMoved = false;
    }
}
