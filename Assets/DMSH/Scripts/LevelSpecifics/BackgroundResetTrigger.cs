using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundResetTrigger : MonoBehaviour
{
    public GameObject backgroundObject;
    public Vector3 backgroundPoint;

    public List<GameObject> gameObjectsDontShow;
    public List<GameObject> gameObjectsShow;
    public Vector3 positionAtTheBegin;

    protected void Start()
    {
        positionAtTheBegin = backgroundObject.transform.position;
    }

    protected void OnTriggerEnter(Collider other)
    {
        foreach (GameObject gameObject in gameObjectsDontShow)
            gameObject?.SetActive(false);

        foreach (GameObject gameObject in gameObjectsShow)
            gameObject?.SetActive(true);

        //backgroundObject.transform.position = backgroundPoint.transform.position;
        backgroundObject.transform.position = new Vector3(backgroundPoint.x,
            backgroundPoint.y, backgroundPoint.z / 1.5f);

    }
}
