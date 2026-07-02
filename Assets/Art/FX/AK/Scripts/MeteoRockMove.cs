using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteoRockMove : MonoBehaviour
{
    public float speed;
    public GameObject rockObj;
    public Vector3 rotDir;
    public float rotSpeed;

    [SerializeField] LayerMask groundLayer;

    public GameObject impactPrefab;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        int layerIndex = (int)Mathf.Log(groundLayer, 2);
        if (collision.gameObject.layer != layerIndex)
            return;

        speed = 0;
        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point;

        if (impactPrefab != null)
        {
            Debug.Log("player: " + collision.gameObject.name);
            var impactVFX = Instantiate(impactPrefab, pos, rot) as GameObject;
            Destroy(impactVFX, 5f);
        }

        Destroy(gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (speed != 0 && rb != null)
        {
            rb.position += transform.forward * (speed * Time.deltaTime);
        }
    }
}
