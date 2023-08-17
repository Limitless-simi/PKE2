using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR;

/**
 * Handler of player input events
 */
public class PlayerEvents : MonoBehaviour
{
    // anchor references.
    public GameObject LeftAnchor;
    public GameObject RightAnchor;
    public GameObject HeadAnchor;

    // game control canvas
    public GameObject exit_restart_canvas;
    // audio sources, will be paused and resumed according to interaction with exit_restart_canvas.
    public AudioSource[] audioSources;

    private Dictionary<InputDevice, GameObject> ControllerSets = null;
    private InputDevice inputSource = new InputDevice();
    private InputDevice controller = new InputDevice();

    public static UnityAction<bool> onHascontroller = null;
    public static UnityAction onTriggerUp = null;
    public static UnityAction onTriggerDown = null;
    public static UnityAction onTouchpadUp = null;
    public static UnityAction onTouchpadDown = null;
    public static UnityAction<InputDevice, GameObject> OnControllerSource = null;

    private bool hasController = false;
    private bool inputActive = true;

    // updated
    public void Awake()
    {
        // Assuming that you have already set up the XR Plugin's input management.
        ControllerSets = CreateControllerSets();
    }

    /**
    // anchor references.
    public GameObject LeftAnchor;
    public GameObject RightAnchor;
    public GameObject HeadAnchor;

    // game control canvas
    public GameObject exit_restart_canvas;
    // audio sources, will be paused and resumed according to interaction with exit_restart_canvas.
    public AudioSource[] audioSources;

    private Dictionary<OVRInput.Controller, GameObject> ControllerSets = null;
    private OVRInput.Controller inputSource = OVRInput.Controller.None;
    private OVRInput.Controller controller = OVRInput.Controller.None;

    public static UnityAction<bool> onHascontroller = null;
    public static UnityAction onTriggerUp = null;
    public static UnityAction onTriggerDown = null;
    public static UnityAction onTouchpadUp = null;
    public static UnityAction onTouchpadDown = null;
    public static UnityAction<OVRInput.Controller, GameObject> OnControllerSource = null;

    private bool hasController = false;
    private bool inputActive = true;
    */

    // updated
    private void OnDestroy()
    {
        if (OVRManager.isHmdPresent)
        {
            OVRManager.HMDMounted -= PlayerFound;
            OVRManager.HMDUnmounted -= PlayerLost;
        }
    }

    void Start()
    {
        OVRManager.HMDMounted += PlayerFound;
        OVRManager.HMDUnmounted -= PlayerLost;
    }

    // updated
    void Update()
    {
        if (!inputActive) return;
        hasController = CheckForController(hasController);

        float triggerValue = 0f;

        // Check if the current controller has the PrimaryIndexTrigger button
        if (controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue) && triggerValue >= 0.5f)
        {
            Debug.Log("Trigger pressed");
        }

        CheckInputSource();
        HandleInput();
    }

    // updated
    private bool CheckForController(bool currentValue)
    {
        InputDevice controllerCheck = controller;

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (var device in devices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                controllerCheck = device;
                break;
            }
        }

        if (controllerCheck == controller)
        {
            foreach (var device in devices)
            {
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                {
                    controllerCheck = device;
                    break;
                }
            }
        }

        controller = UpdateSource(controllerCheck, controller);

        return true;
    }

    // updated
    private void CheckInputSource()
    {
        if (controller != null)
        {
            if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
            {
                Debug.Log("Primary button pressed");
                Debug.Log("Controller check: " + controller);
            }

            if (controller.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue) && secondaryButtonValue)
            {
                Debug.Log("Secondary button pressed");
                Debug.Log("Controller check: " + controller);
            }
        }

        inputSource = UpdateSource(controller, inputSource);
    }


    private void PlayerFound()
    {
        Debug.Log("Input active is set to true");
        inputActive = true;
    }
    private void PlayerLost()
    {
        inputActive = false;
    }

    // updated
    private Dictionary<InputDevice, GameObject> CreateControllerSets()
    {
        Dictionary<InputDevice, GameObject> newSets = new Dictionary<InputDevice, GameObject>();

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (var device in devices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                newSets.Add(device, RightAnchor);
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                newSets.Add(device, LeftAnchor);
            }
        }

        newSets.Add(InputDevices.GetDeviceAtXRNode(XRNode.Head), HeadAnchor);

        return newSets;
    }

    // updated
    public InputDevice UpdateSource(InputDevice check, InputDevice previous)
    {
        if (check == previous)
            return previous;

        GameObject controllerObject = null;
        ControllerSets.TryGetValue(check, out controllerObject);

        if (controllerObject == null)
            controllerObject = HeadAnchor;

        Debug.Log(controllerObject.name); // Use Debug.Log instead of print
        if (OnControllerSource != null)
            OnControllerSource(check, controllerObject);

        return check;
    }

    // updated
    private void HandleInput()
    {
        if (controller != null)
        {
            if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonValue))
            {
                if (triggerButtonValue)
                {
                    if (onTriggerDown != null)
                    {
                        onTriggerDown();
                    }
                }
                else
                {
                    if (onTriggerUp != null)
                    {
                        onTriggerUp();
                    }
                }
            }

            if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue))
            {
                if (primaryButtonValue)
                {
                    if (onTouchpadDown != null)
                    {
                        onTouchpadDown();
                    }
                }
                else
                {
                    if (onTouchpadUp != null)
                    {
                        onTouchpadUp();
                    }
                }
            }

            if (controller.TryGetFeatureValue(CommonUsages.menuButton, out bool menuButtonValue) && menuButtonValue)
            {
                ToggleCanvas();
            }
        }
    }

    // new
    private void ToggleCanvas()
    {
        Canvas canvas = exit_restart_canvas.GetComponent<Canvas>();

        if (canvas.enabled)
        {
            canvas.enabled = false;
            Time.timeScale = 1;

            foreach (AudioSource audio in audioSources)
            {
                audio.UnPause();
            }
        }
        else
        {
            canvas.enabled = true;
            Time.timeScale = 0;

            foreach (AudioSource audio in audioSources)
            {
                audio.Pause();
            }
        }
    }
}
