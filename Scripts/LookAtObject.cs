using UnityEngine;

public class LookAtObject : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Controller>() != null)
            other.GetComponent<Controller>().LookAt(this.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Controller>() != null)
            if (other.GetComponent<Controller>().lookAt == this.transform)
                other.GetComponent<Controller>().LookAt(null);
    }
}
