using UnityEngine;
using System.Collections;

public class ManualController : MonoBehaviour
{

    public float movementSpeed = 6;
    public float lastRotAngle;
    public float lastMovementSpeed;
    public bool controllEnabled = true;

    Rigidbody rb;
    //Camera viewCamera;
    Vector3 velocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //viewCamera = Camera.main;
    }

    void Update()
    {

        //Vector3 mousePos = viewCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, viewCamera.transform.position.y));
       // print(op+Vector3.Angle(transform.forward, new Vector3(0, 0, 1)));
       
            //transform.LookAt(mousePos + Vector3.up * transform.position.y);
        //velocity = new Vector3(0, 0, Input.GetAxisRaw("Vertical")).normalized * moveSpeed;
        velocity = transform.forward * Input.GetAxisRaw("Vertical") * movementSpeed;
        lastMovementSpeed = Input.GetAxisRaw("Vertical") * movementSpeed;

    }

    void FixedUpdate()
    {
        if (controllEnabled) {
            Quaternion deltaRotation;
            if (Input.GetAxisRaw("Vertical") >= 0)
            {
                lastRotAngle = Input.GetAxisRaw("Horizontal");
            }
            else
            {
                lastRotAngle = -Input.GetAxisRaw("Horizontal");
            }
            deltaRotation = Quaternion.Euler(0.0f, lastRotAngle, 0.0f);
                //Quaternion deltaRotation = Quaternion.Euler(0.0f, Input.GetAxisRaw("Horizontal"),0.0f);

            rb.MoveRotation(rb.rotation * deltaRotation);
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

    }
}
