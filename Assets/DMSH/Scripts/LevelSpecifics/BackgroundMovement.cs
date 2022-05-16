using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMovement : MonoBehaviour
{
    public float speed = 3.0f;
    public SectionObject backgroundObject;

    protected void Start()
    {

    }

    protected void Update()
    {
        backgroundObject.plane.transform.position = new Vector3(backgroundObject.plane.transform.position.x, backgroundObject.plane.transform.position.y, backgroundObject.plane.transform.position.z - speed * Time.deltaTime);
        backgroundObject.planeEndPoint = new Vector3(backgroundObject.planeEndPoint.x, backgroundObject.planeEndPoint.y, backgroundObject.planeEndPoint.z - speed * Time.deltaTime);
        backgroundObject.planeMiddlePoint = new Vector3(backgroundObject.planeMiddlePoint.x, backgroundObject.planeMiddlePoint.y, backgroundObject.planeMiddlePoint.z - speed * Time.deltaTime);
    }
}
