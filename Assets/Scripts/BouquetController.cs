using UnityEngine;
using UnityEngine.InputSystem;

// drag a handle to reshape the bouquet, drag anywhere else to turn it
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class BouquetController : MonoBehaviour
{
    private const float PickRadiusPixels = 34.0f;
    private const float OrbitPerPixel = 0.32f;
    private const float PitchLimit = 62.0f;
    private const float IdleHideDelay = 2.4f;
    private const float EaseSharpness = 6.0f;

    public BouquetBuilder builder;
    public MeshFilter dialMesh;
    public MeshRenderer dialRenderer;

    public Vector3 orbitTarget = new Vector3(0.0f, 0.04f, 0.0f);
    public float orbitRadius = 3.45f;
    public float yaw;
    public float pitch = 12.0f;

    public Dial[] dials = BouquetDialSet.Defaults();

    private int heldDial = -1;
    private bool turning;
    private Vector2 lastPointer;
    private float idleSeconds;
    private Mesh dialGeometry;
    private BouquetSettings easeTarget;
    private float paletteTarget;

    private void OnEnable()
    {
        RebuildDials();
        PlaceCamera();
    }

    private void OnValidate()
    {
        RebuildDials();
        PlaceCamera();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EaseBouquet();

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 pointer = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            heldDial = BouquetDialSet.Pick(dials, GetComponent<Camera>(), pointer, PickRadiusPixels);
            turning = heldDial < 0;
            lastPointer = pointer;
            idleSeconds = 0.0f;
        }

        if (mouse.leftButton.isPressed)
        {
            if (heldDial >= 0)
            {
                float next = BouquetDialSet.ValueUnderPointer(dials[heldDial], GetComponent<Camera>(), pointer);
                if (!Mathf.Approximately(next, dials[heldDial].value))
                {
                    dials[heldDial].value = next;
                    ApplyDials();
                }
            }
            else if (turning)
            {
                Vector2 delta = pointer - lastPointer;
                yaw -= delta.x * OrbitPerPixel;
                pitch = Mathf.Clamp(pitch + delta.y * OrbitPerPixel, -PitchLimit, PitchLimit);
                PlaceCamera();
                RebuildDials();
            }

            lastPointer = pointer;
            idleSeconds = 0.0f;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            heldDial = -1;
            turning = false;
        }

        idleSeconds += Time.deltaTime;
        if (dialRenderer != null)
        {
            dialRenderer.enabled = idleSeconds < IdleHideDelay;
        }
    }

    public void PlaceCamera()
    {
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0.0f);
        transform.position = orbitTarget + orbit * (Vector3.back * orbitRadius);
        transform.rotation = Quaternion.LookRotation(orbitTarget - transform.position, Vector3.up);
    }

    public void ApplyDials()
    {
        if (builder == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            BouquetDialSet.Apply(dials, builder);
            builder.Rebuild();
            RebuildDials();
            return;
        }

        easeTarget = builder.settings.Clone();
        paletteTarget = BouquetDialSet.Resolve(dials, easeTarget);
        RebuildDials();
    }

    private void EaseBouquet()
    {
        if (easeTarget == null || builder == null)
        {
            return;
        }

        float blend = 1.0f - Mathf.Exp(-Time.deltaTime * EaseSharpness);
        bool isMoving = builder.settings.EaseToward(easeTarget, blend);
        builder.palette = BouquetSettings.Ease(builder.palette, paletteTarget, blend, ref isMoving);

        if (isMoving)
        {
            builder.Rebuild();
        }
        else
        {
            easeTarget = null;
        }
    }

    public void RebuildDials()
    {
        if (dialMesh == null)
        {
            return;
        }

        if (dialGeometry == null)
        {
            dialGeometry = new Mesh { name = "BouquetDials" };
            dialGeometry.hideFlags = HideFlags.DontSave;
        }

        MeshBuffer buffer = new MeshBuffer(isShaded: false);
        Vector3 tie = builder != null ? new Vector3(0.0f, builder.settings.bindHeight, 0.0f) : Vector3.zero;
        BouquetDialSet.BuildMesh(buffer, dials, tie, DialTrack, DialHandle);
        buffer.WriteTo(dialGeometry);
        dialMesh.sharedMesh = dialGeometry;
    }

    private static readonly Color DialTrack = new Color(0.86f, 0.87f, 0.88f, 1.0f);
    private static readonly Color DialHandle = new Color(1.0f, 1.0f, 1.0f, 1.0f);
}
