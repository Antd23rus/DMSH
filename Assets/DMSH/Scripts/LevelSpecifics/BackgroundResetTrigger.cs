using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundResetTrigger : MonoBehaviour
{
    public SectionObject backgroundObject;
    public Vector3 backgroundPoint;
    
    protected void Start()
    {

    }

    protected void OnTriggerEnter(Collider other)
    {
        if (backgroundObject == null)
            return;
        
        backgroundObject.plane.transform.position = new Vector3(backgroundPoint.x,
            backgroundPoint.y, backgroundPoint.z / 1.5f);

        //backgroundObject.planeEndPoint = new Vector3(backgroundObject.plane.transform.position.x,
        //    backgroundObject.plane.transform.position.y,
        //    (backgroundObject.plane.transform.localScale.z * 5 * (backgroundObject.index + 1)) + (backgroundObject.index * 100)); 
        //
        //backgroundObject.planeMiddlePoint = new Vector3(backgroundObject.plane.transform.position.x,
        //    backgroundObject.plane.transform.position.y,
        //    backgroundObject.plane.transform.position.z + (backgroundObject.plane.transform.localScale.z * 10 * backgroundObject.index));
        //
    }
}
