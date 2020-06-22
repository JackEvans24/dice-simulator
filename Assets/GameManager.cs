using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform mainCamera;

    [Header("Dice")]
    public Rigidbody[] diePrefabs;
    [Header("Pre-Throw")]
    private int diePrefabIndex;
    public Transform diceStartPos;
    public Vector3 diceStartRotation;
    public float dieXRotation;
    public float dieYRotation;
    private float dieZRotation;
    public Unity.Mathematics.float2 dieZRotationBounds;
    public float dieZRotationIncrement;
    private bool dieRotationAscending;

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

    [Header("UI")]
    public Canvas preThrowCanvas;
    public Canvas postThrowCanvas;

    void Start()
    {
        originalCameraPos = mainCamera.position;
        originalCameraRotation = mainCamera.rotation.eulerAngles;

        InstantiateDie();

        canThrow = true;
    }

    void InstantiateDie()
    {
        if (currentDie != null)
        {
            var dice = GameObject.FindGameObjectsWithTag("Die");
            foreach (var die in dice)
                GameObject.Destroy(die);
        }

        var diePrefab = diePrefabs[diePrefabIndex];
        currentDie = GameObject.Instantiate(diePrefab, diceStartPos.position, Quaternion.Euler(diceStartRotation));
    }

    // Update is called once per frame
    void Update()
    {
        if (canThrow && Input.GetButtonDown("Quit"))
            Application.Quit();

        if (thrown)
            CheckDieIsAtRest();
        else if (!throwing && !thrown)
        {
            if (canThrow)
            {
                if (Input.GetButtonDown("Jump"))
                {
                    throwing = true;
                    canThrow = false;
                    return;
                }
                else if (Input.GetButtonDown("Next") || Input.GetButtonDown("Previous"))
                {
                    ChangeDie(Input.GetButtonDown("Next"));
                }
            }

            CalculateRotation();
        }

        if (waitingToSettle && currentSettleWaitTime <= settleWaitTime)
            currentSettleWaitTime += Time.deltaTime;

        if (!cameraMoved && currentSettleWaitTime > settleWaitTime)
            MoveCamera();

        if (canReset && Input.GetButtonDown("Jump"))
            Reset();
    }

    void ChangeDie(bool next)
    {
        diePrefabIndex += next ? 1 : -1;

        if (diePrefabIndex >= diePrefabs.Length)
            diePrefabIndex = 0;
        else if (diePrefabIndex < 0)
            diePrefabIndex = diePrefabs.Length - 1;

        InstantiateDie();
    }

    void CalculateRotation()
    {
        if (dieRotationAscending)
            dieZRotation += dieZRotationIncrement;
        else
            dieZRotation -= dieZRotationIncrement;

        if (dieZRotation <= dieZRotationBounds.x || dieZRotation >= dieZRotationBounds.y)
            dieRotationAscending = !dieRotationAscending;
    }

    void FixedUpdate()
    {
        if (!throwing)
        {
            currentDie.angularVelocity = new Vector3(
                dieXRotation,
                dieYRotation,
                dieZRotation
            );
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

        preThrowCanvas.gameObject.SetActive(false);

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
            .setOnComplete(() => {
                postThrowCanvas.gameObject.SetActive(true);

                canReset = true;
            });

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

        postThrowCanvas.gameObject.SetActive(false);

        Tween(mainCamera.gameObject, originalCameraPos, originalCameraRotation, cameraMoveDuration)
            .setOnComplete(() => {
                preThrowCanvas.gameObject.SetActive(true);

                canThrow = true;
            });

        waitingToSettle = false;
        currentSettleWaitTime = 0;

        Tween(currentDie.gameObject, diceStartPos.position, diceStartRotation, cameraMoveDuration)
            .setOnComplete(() => currentDie.constraints = RigidbodyConstraints.FreezePosition);

        throwing = thrown = cameraMoved = false;
    }
}
