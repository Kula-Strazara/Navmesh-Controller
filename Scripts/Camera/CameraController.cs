using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//triba camera i char intercollission
//onda triba dodat fensy towere za lookat objekte i malo poslozit scenu i snimit mali 20s video kako se krecen pa switchan controller
//i onda s njin trcin

//4.ti video je iskljucivo prediction i razne stvari vezane za prediction + eventualno ako se sitin neke sitnice 
//5.ti je spojit chara i animacije
//6.ti moze bit ragdoll i kill mehanika
// intro video moze bit snimka iz igre

//outro videii za svakog mogu bit malo snippet iz sljedeceg
//npr vid 1 ce imat 2x controller
//vid 2 ce imat kamera system i look at
//vid 3 ce imat prediction snippet
//vid 4 animiranog lika
//vid 5 ce imat ragdoll smrt

//svaki taj snippet moze bit short



public class CameraController : MonoBehaviour
{
    public static CameraController controller { set; get; }

    private Transform cameraTarget;
    public float speed = 5f;
    void Awake()
    {
        if (controller == null)
            controller = this;
        else
            Destroy(this.gameObject);
    }


    [Range(0f, 10f)]
    public float cameraMovementOffset = 2f;
    private Vector3 cameraMoveVector = Vector3.zero;
    void Update()
    {
        if (cameraTarget == null)
            return;

        // target
        Vector3 targetSpeed = cameraTarget.position - this.transform.position;
        // clamp and add offset
        targetSpeed = targetSpeed.normalized * Mathf.Clamp(targetSpeed.magnitude - cameraMovementOffset, 0f, 10f);
        //smoothing
        cameraMoveVector += (targetSpeed - cameraMoveVector) * Time.deltaTime * 4f;
        //move camera controller
        this.transform.position += cameraMoveVector * Time.deltaTime * speed;
    }

    public void SetTarget(Transform targ)
    {
        cameraTarget = targ;
    }
}
