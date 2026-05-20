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

    private Coroutine _panCameraCoroutine;

    private GameObject _currentCamera;
    private Component _positionComposer;
    private FieldInfo _dampingField;

    private System.Reflection.FieldInfo _targetOffsetField;
    private Vector3 _startingTrackedObjectOffset;

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
                _currentCamera = _allVirtualCameras[i];
                _positionComposer = FindComponentByName(_currentCamera, "CinemachinePositionComposer");

                if (_positionComposer == null)
                {
                    Debug.LogError("No CinemachinePositionComposer found on " + _currentCamera.name);
                    return;
                }

                _dampingField = _positionComposer.GetType().GetField("Damping");

                if (_dampingField == null)
                {
                    Debug.LogError("Could not find Damping field on " + _positionComposer.GetType().Name);
                    return;
                }

                _targetOffsetField = _positionComposer.GetType().GetField("TargetOffset");

                if (_targetOffsetField == null)
                {
                    _targetOffsetField = _positionComposer.GetType().GetField("m_TrackedObjectOffset");
                }

                if (_targetOffsetField == null)
                {
                    Debug.LogError("Could not find TargetOffset or m_TrackedObjectOffset on " + _positionComposer.GetType().Name);
                    return;
                }

                _normYPanAmount = GetYDamping();
                _startingTrackedObjectOffset = GetTrackedObjectOffset();
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

    #region Pan Camera

    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        if (_positionComposer == null || _targetOffsetField == null)
        {
            return;
        }

        if (_panCameraCoroutine != null)
        {
            StopCoroutine(_panCameraCoroutine);
        }

        _panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector3 endPos = Vector3.zero;
        Vector3 startingPos = Vector3.zero;

        if (!panToStartingPos)
        {
            switch (panDirection)
            {
                case PanDirection.Up:
                    endPos = Vector3.up;
                    break;

                case PanDirection.Down:
                    endPos = Vector3.down;
                    break;

                case PanDirection.Left:
                    endPos = Vector3.right;
                    break;

                case PanDirection.Right:
                    endPos = Vector3.left;
                    break;
            }

            endPos *= panDistance;
            startingPos = _startingTrackedObjectOffset;
            endPos += startingPos;
        }
        else
        {
            startingPos = GetTrackedObjectOffset();
            endPos = _startingTrackedObjectOffset;
        }

        float elapsedTime = 0f;

        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;

            Vector3 panLerp = Vector3.Lerp(startingPos, endPos, elapsedTime / panTime);
            SetTrackedObjectOffset(panLerp);

            yield return null;
        }
    }

    private Vector3 GetTrackedObjectOffset()
    {
        return (Vector3)_targetOffsetField.GetValue(_positionComposer);
    }

    private void SetTrackedObjectOffset(Vector3 offset)
    {
        _targetOffsetField.SetValue(_positionComposer, offset);
    }

    #endregion

    #region Swap Cameras

    public void SwapCamera(GameObject cameraFromLeft, GameObject cameraFromRight, Vector2 triggerExitDirection)
    {
        if (_currentCamera == cameraFromLeft && triggerExitDirection.x > 0f)
        {
            cameraFromRight.SetActive(true);
            cameraFromLeft.SetActive(false);

            SetCurrentCamera(cameraFromRight);
        }
        else if (_currentCamera == cameraFromRight && triggerExitDirection.x < 0f)
        {
            cameraFromLeft.SetActive(true);
            cameraFromRight.SetActive(false);

            SetCurrentCamera(cameraFromLeft);
        }
    }

    private void SetCurrentCamera(GameObject newCamera)
    {
        _currentCamera = newCamera;
        _positionComposer = FindComponentByName(_currentCamera, "CinemachinePositionComposer");

        if (_positionComposer == null)
        {
            Debug.LogError("No CinemachinePositionComposer found on " + _currentCamera.name);
            return;
        }

        _dampingField = _positionComposer.GetType().GetField("Damping");
        _targetOffsetField = _positionComposer.GetType().GetField("TargetOffset");

        if (_targetOffsetField == null)
        {
            _targetOffsetField = _positionComposer.GetType().GetField("m_TrackedObjectOffset");
        }

        _startingTrackedObjectOffset = GetTrackedObjectOffset();
    }

    #endregion
}
