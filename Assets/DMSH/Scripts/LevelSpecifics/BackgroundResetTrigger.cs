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
        Debug.Log($"{backgroundObject.plane.name} {backgroundPoint}");
        //backgroundObject.transform.position = backgroundPoint.transform.position;
        backgroundObject.plane.transform.position = new Vector3(backgroundPoint.x,
            backgroundPoint.y, backgroundPoint.z / 1.5f);

        //backgroundObject.interiorObjectsRoot.transform.position = backgroundObject.plane.transform.position;


    }
}
