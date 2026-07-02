using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaySpawner : MonoBehaviour 
{
    public Transform pivot;
    public float speed = 15f;
    public float drug = 1f;
    public float repeatingTime = 1f;
    public GameObject craterPrefab;
    public float spawnRate = 1f;
    public float spawnDuration = 1f;

    private float startSpeed = 0f;
    private float spawnDur;
    private Vector3 stepPosition;

    private void OnEnable()
    {
        startSpeed = speed;
        transform.position = pivot.position;
        stepPosition = pivot.position;
        spawnDur = spawnDuration;
    }

    void Start()
    {
        //InvokeRepeating("StartAgain", 0f, repeatingTime);
    }

    void StartAgain()
    {
        startSpeed = speed;
        transform.position = pivot.position;
        stepPosition = pivot.position;
        spawnDur = spawnDuration;
    }

    void FixedUpdate()
    {
        spawnDur -= Time.deltaTime;
        startSpeed = startSpeed * drug;
        transform.position += transform.forward * (startSpeed * Time.deltaTime);

        var heading = transform.position - stepPosition;
        var distance = heading.magnitude;
        if (distance >= spawnRate && spawnDur > 0)
        {
            if (craterPrefab != null)
            {
                Vector3 pos = transform.position;
                if (Terrain.activeTerrain != null)
                {
                    pos.y = Terrain.activeTerrain.SampleHeight(transform.position);
                }
                var craterInstance = Instantiate(craterPrefab, pos, Quaternion.identity);
                var craterPs = craterInstance.GetComponent<ParticleSystem>();
                if (craterPs != null)
                {
                    Destroy(craterInstance, craterPs.main.duration);
                }
                else
                {
                    var flashPsParts = craterInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
                    Destroy(craterInstance, flashPsParts.main.duration);
                }
            }
            //distance = 0;
            stepPosition = transform.position;
        }
    }
}
