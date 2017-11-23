using UnityEngine;
using System.Collections;

public class ExternalController : MonoBehaviour
{

    public float movementSpeed = 6;
    public float lastRotAngle;
    public float lastMovementSpeed;
    public bool controllEnabled = true;

    Rigidbody rb;
    Vector3 velocity;

    int vertical = 0;
    int horizontal = 0;


    public void setHorizontal(int val){
        horizontal = val;
    }

    public void setVertical(int val) {
        vertical = val;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //viewCamera = Camera.main;
    }

    void Update()
    {

        velocity = transform.forward * vertical * movementSpeed;
        lastMovementSpeed = vertical * movementSpeed;

    }

    void FixedUpdate()
    {
        if (controllEnabled) {
            Quaternion deltaRotation;
            if (vertical >= 0)
            {
                lastRotAngle = horizontal;
            }
            else
            {
                lastRotAngle = -horizontal;
            }
            deltaRotation = Quaternion.Euler(0.0f, lastRotAngle, 0.0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

    }
}
