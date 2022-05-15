using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMovement : MonoBehaviour
{
    public float speed = 3.0f;
    public GameObject backgroundObject;

    protected void Start()
    {
        backgroundObject = gameObject;
    }

    protected void Update()
    {
        backgroundObject.transform.position = new Vector3(backgroundObject.transform.position.x, backgroundObject.transform.position.y, backgroundObject.transform.position.z - speed * Time.deltaTime);
    }
}
