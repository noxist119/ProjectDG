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
		base.transform.position = pivot.position;
		stepPosition = pivot.position;
		spawnDur = spawnDuration;
	}

	private void Start()
	{
	}

	private void StartAgain()
	{
		startSpeed = speed;
		base.transform.position = pivot.position;
		stepPosition = pivot.position;
		spawnDur = spawnDuration;
	}

	private void FixedUpdate()
	{
		spawnDur -= Time.deltaTime;
		startSpeed *= drug;
		base.transform.position += base.transform.forward * (startSpeed * Time.deltaTime);
		float distance = (base.transform.position - stepPosition).magnitude;
		if (!(distance >= spawnRate) || !(spawnDur > 0f))
		{
			return;
		}
		if (craterPrefab != null)
		{
			Vector3 pos = base.transform.position;
			if (Terrain.activeTerrain != null)
			{
				pos.y = Terrain.activeTerrain.SampleHeight(base.transform.position);
			}
			GameObject craterInstance = Object.Instantiate(craterPrefab, pos, Quaternion.identity);
			ParticleSystem craterPs = craterInstance.GetComponent<ParticleSystem>();
			if (craterPs != null)
			{
				Object.Destroy(craterInstance, craterPs.main.duration);
			}
			else
			{
				ParticleSystem flashPsParts = craterInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
				Object.Destroy(craterInstance, flashPsParts.main.duration);
			}
		}
		stepPosition = base.transform.position;
	}
}
