using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera target;
    private readonly float startingScale = 2.5f;
    private readonly float startingSpeed = 5.0f;
    private float zoomSpeed = 5.0f;
    private float moveSpeed = 5.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(this.Zoom());
        StartCoroutine(this.Move());
    }

    IEnumerator Move() {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");
        Vector3 dir = target.transform.up * zInput + target.transform.right * xInput;
        target.transform.position += dir * moveSpeed * Time.deltaTime;
        yield return null;
    }

    IEnumerator Zoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        target.orthographicSize = Mathf.Max(startingScale, target.orthographicSize + scroll * zoomSpeed);
        moveSpeed = startingSpeed * (target.orthographicSize / startingScale);
        yield return null;
    }

}
