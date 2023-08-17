﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

public class Pointer : MonoBehaviour
{
    // distance that this pointer does ray casts on.
    public float distance = 10.0f;
    // line renderer to show current pointing action.
    public LineRenderer lineRenderer = null;
    // layer used when ray casting
    public LayerMask everythingMask = 0;
    // layer used when ray casting
    public LayerMask interactibleMask = 0;
    // origin of pointer
    public Transform currentOrigin;
    // called when pointer is updating location and rotation.
    public UnityAction<Vector3, GameObject> OnPointerUpdate = null;
    // object that this pointer is pointing on (fruit, toygun...)
    public GameObject currentObject = null;

    public GameObject attachedObject = null;
    public GameObject handAnchor;
    public GameObject MessageCanvas;
    public Text MessageContent;
    private float WhenWasMessageCanvasLatelyActivated = 0f;

    // updated?
    private void Awake()
    {
        // PlayerEvents.OnControllerSource += UpdateOrigin;
        PlayerEvents.onTriggerDown += ProcessTriggerDown;
        PlayerEvents.onTouchpadDown += ProcessTouchpadDown;
    }


    void Start()
    {

    }

    void Update()
    {
        Vector3 hitPoint = UpdateLine();
        GameObject lastObj = currentObject;
        currentObject = UpdatePointerStatus();
        if (lastObj != null && lastObj != currentObject)
        {
            VRObject obj = lastObj.GetComponent<VRObject>();
            if (obj != null)
            {
                if (obj is VRDraggableObject)
                {
                    VRDraggableObject draggable = (VRDraggableObject)obj;
                    VRDraggableObjectTarget holder = draggable.gameObject.transform.parent.gameObject.GetComponent<VRDraggableObjectTarget>();
                    if (holder != null && holder.gameObject != draggable.defaultHolder)
                    {
                        if (holder.containsType(draggable.type))
                            obj.applyHighlight(HighlightOptions.correct);
                        else
                        {
                            obj.applyHighlight(HighlightOptions.wrong);
                        }
                    }
                    else
                    {
                        obj.applyHighlight(HighlightOptions.none);
                    }
                }
                else
                {/* if(obj.currentHighlightOption == High)*/
                    obj.applyHighlight(HighlightOptions.none);
                }
            }
        }

        // checking if the object which is selected by pointer is a possible target for a draggable object and if true, call the interaction for that target.
        if (currentObject != null)
        {
            // if the pointer points on VRObject, then highlight that object
            VRObject obj = currentObject.GetComponent<VRObject>();
            if (obj != null) obj.applyHighlight(HighlightOptions.hover);

            //if (attachedObject != null)
            //{
            //    VRDraggableObjectTarget target = currentObject.GetComponent<VRDraggableObjectTarget>();
            //    if (target != null)
            //    {
            //        target.react(this);
            //    }
            //}
        }

        if (OnPointerUpdate != null)
            OnPointerUpdate(hitPoint, currentObject);

        if (Input.GetKeyDown("space"))
        {
            
            if (!currentObject)
            {
                print("no object is interactible");
                return;
            }
            Interactible interactible = currentObject.GetComponent<Interactible>();
            if (interactible)
            {
                interactible.Pressed();
                print("interactible pressed");
            }
            else
            {
                VRObject obj = currentObject.GetComponent<VRObject>();
                if (obj != null)
                {
                    obj.interact();
                    obj.interact(this);
                }
            }
        }
    }

    public Vector3 UpdateLine()
    {
        RaycastHit hit = CreateRaycast(everythingMask);
        SetLinecolor();
        if (currentOrigin == null) return Vector3.zero;
        Vector3 endPosition = currentOrigin.position + currentOrigin.forward * distance;
        if (hit.collider != null)
            endPosition = hit.point;
        lineRenderer.SetPosition(0, currentOrigin.position);
        lineRenderer.SetPosition(1, endPosition);
        return endPosition;
    }

    private RaycastHit CreateRaycast(int layer)
    {
        RaycastHit hit;
        if (currentOrigin != null)
        {
            Ray ray = new Ray(currentOrigin.position, currentOrigin.forward);
            Physics.Raycast(ray, out hit, distance, layer);
            return hit;
        }
        else
            print("controller not set yet");
        return new RaycastHit();
    }

    private void SetLinecolor()
    {
        if (!lineRenderer) return;
        Color endColor = Color.white;
        endColor.a = 0.0f;
        lineRenderer.endColor = endColor;
        Color startColor = Color.white;
        startColor.a = 1.0f;
        lineRenderer.startColor = startColor;
    }

    // updated?
    private void OnDestroy()
    {
        // PlayerEvents.OnControllerSource -= UpdateOrigin;
        PlayerEvents.onTriggerDown -= ProcessTriggerDown;
        PlayerEvents.onTouchpadDown -= ProcessTouchpadDown;
    }


    // updated

    /**private void UpdateOrigin(OVRInput.Controller controller, GameObject controllerObject)
    {
        /**currentOrigin = controllerObject.transform;
        if (controller == OVRInput.Controller.Touchpad)
        {
            lineRenderer.enabled = false;
        }
        else
        {
            lineRenderer.enabled = true;
        }
       

    }**/



    private void UpdateOrigin(XRController controller, GameObject controllerObject)
    {
        //currentOrigin = controllerObject.transform;

        if (controller.controllerNode == XRNode.LeftHand || controller.controllerNode == XRNode.RightHand)
        {
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
    


    private GameObject UpdatePointerStatus()
    {
        RaycastHit hit = CreateRaycast(interactibleMask);
        if (hit.collider)
            return hit.collider.gameObject;
        return null;
    }

    private void ProcessTriggerDown()
    {
        Debug.Log("Processing Trigger Down");
        if (!currentObject) return;
        //Interactible interactible = currentObject.GetComponent<Interactible>();
        //interactible.Pressed();

        Interactible interactible = currentObject.GetComponent<Interactible>();
        if (interactible)
        {
            interactible.Pressed();
            print("interactible pressed");
        }
        else
        {
            VRObject obj = currentObject.GetComponent<VRObject>();
            if (obj != null)
            {
                obj.interact();
                obj.interact(this);
            }
        }
    }

    private void ProcessTouchpadDown()
    {
        if (!currentObject)
            return;
        Interactible interactible = currentObject.GetComponent<Interactible>();
        interactible.Pressed();
    }

    public void DragNoParenting(GameObject obj)
    {
        attachedObject = obj;
        VRObject vrObject = attachedObject.GetComponent<VRObject>();
        vrObject.Parent(attachedObject.transform);
        BoxCollider box = attachedObject.GetComponent<BoxCollider>();
        if (box != null) box.enabled = false;
    }

    public void DropNoParenting(GameObject holder)
    {
        VRObject vrObject = attachedObject.GetComponent<VRObject>();
        vrObject.UnParent();
        BoxCollider box = attachedObject.GetComponent<BoxCollider>();
        if (box != null) box.enabled = true;
        attachedObject = null;
    }

    public void Drag(GameObject obj)
    {
        if (attachedObject == null)
        {
            attachedObject = obj;
            attachedObject.transform.parent = handAnchor.transform;
            attachedObject.transform.localPosition = Vector3.zero;
            attachedObject.transform.localEulerAngles = new Vector3(0, 0, 0);
            BoxCollider box = attachedObject.GetComponent<BoxCollider>();
            if (box != null) box.enabled = false;
        }
        else
        {
            //MessageContent.text = ;
            //MessageCanvas.SetActive(true);
            //StartCoroutine(HideMessageAfterDelay(3));
            //WhenWasMessageCanvasLatelyActivated = Time.time;
            DisplayMessage(
                "You can not drag more than one object at the same time.",
                3
                );
        }
    }

    public void Drop(GameObject holder)
    {
        Debug.Log("Trying to drop object");
        Vector2 oldSize = attachedObject.GetComponent<RectTransform>().sizeDelta;
        attachedObject.transform.parent = holder.transform;
        attachedObject.transform.localPosition = Vector3.zero;
        attachedObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        attachedObject.transform.localScale = new Vector3(1, 1, 1);
        attachedObject.GetComponent<RectTransform>().sizeDelta = oldSize;
        BoxCollider box = attachedObject.GetComponent<BoxCollider>();
        if (box != null) box.enabled = true;
        attachedObject = null;
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (WhenWasMessageCanvasLatelyActivated + delay > Time.time)
            StartCoroutine(HideMessageAfterDelay(WhenWasMessageCanvasLatelyActivated + delay - Time.time));
        else if (MessageCanvas != null)
            MessageCanvas.SetActive(false);
    }

    public void DisplayMessage(string msg, float delay)
    {
        if (MessageCanvas != null)
        {
            MessageContent.text = msg;
            MessageCanvas.SetActive(true);
        }
        WhenWasMessageCanvasLatelyActivated = Time.time;
        StartCoroutine(HideMessageAfterDelay(delay));
    }
}
