using System.Collections;
using System.Reflection;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private GameObject[] _allVirtualCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    public float _fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine _lerpYPanCoroutine;

    private Component _positionComposer;
    private FieldInfo _dampingField;
    private float _normYPanAmount;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < _allVirtualCameras.Length; i++)
        {
            if (_allVirtualCameras[i].activeInHierarchy)
            {
                _positionComposer = FindComponentByName(_allVirtualCameras[i], "CinemachinePositionComposer");

                if (_positionComposer == null)
                {
                    Debug.LogError("No CinemachinePositionComposer found on " + _allVirtualCameras[i].name);
                    return;
                }

                _dampingField = _positionComposer.GetType().GetField("Damping");

                if (_dampingField == null)
                {
                    Debug.LogError("Could not find Damping field on " + _positionComposer.GetType().Name);
                    return;
                }

                _normYPanAmount = GetYDamping();
                return;
            }
        }
    }

    public void LerpYDamping(bool isPlayerFalling)
    {
        if (_positionComposer == null || _dampingField == null)
        {
            return;
        }

        if (_lerpYPanCoroutine != null)
        {
            StopCoroutine(_lerpYPanCoroutine);
        }

        _lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        float startDampAmount = GetYDamping();
        float endDampAmount = 0f;

        if (isPlayerFalling)
        {
            endDampAmount = _fallPanAmount;
            LerpedFromPlayerFalling = true;
        }
        else
        {
            endDampAmount = _normYPanAmount;
        }

        float elapsedTime = 0f;

        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallYPanTime);
            SetYDamping(lerpedPanAmount);

            yield return null;
        }

        IsLerpingYDamping = false;
    }

    private float GetYDamping()
    {
        Vector3 damping = (Vector3)_dampingField.GetValue(_positionComposer);
        return damping.y;
    }

    private void SetYDamping(float yDamping)
    {
        Vector3 damping = (Vector3)_dampingField.GetValue(_positionComposer);
        damping.y = yDamping;
        _dampingField.SetValue(_positionComposer, damping);
    }

    private Component FindComponentByName(GameObject root, string typeName)
    {
        Component[] components = root.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetType().Name == typeName)
            {
                return components[i];
            }
        }

        return null;
    }
}