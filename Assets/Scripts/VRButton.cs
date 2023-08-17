using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VRButton : VRObject
{
    Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
    // Start is called before the first frame update
    public override void interact()
    {
        
    }

    public override void interact(Pointer pointer)
    {
        base.interact(pointer);
        button.onClick.Invoke();
    }
}
