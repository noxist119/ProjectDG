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
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		startSpeed = speed;
		((Component)this).transform.position = pivot.position;
		stepPosition = pivot.position;
		spawnDur = spawnDuration;
	}

	private void Start()
	{
	}

	private void StartAgain()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		startSpeed = speed;
		((Component)this).transform.position = pivot.position;
		stepPosition = pivot.position;
		spawnDur = spawnDuration;
	}

	private void FixedUpdate()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		spawnDur -= Time.deltaTime;
		startSpeed *= drug;
		Transform transform = ((Component)this).transform;
		transform.position += ((Component)this).transform.forward * (startSpeed * Time.deltaTime);
		Vector3 val = ((Component)this).transform.position - stepPosition;
		float magnitude = ((Vector3)(ref val)).magnitude;
		if (!(magnitude >= spawnRate) || !(spawnDur > 0f))
		{
			return;
		}
		if ((Object)(object)craterPrefab != (Object)null)
		{
			Vector3 position = ((Component)this).transform.position;
			if ((Object)(object)Terrain.activeTerrain != (Object)null)
			{
				position.y = Terrain.activeTerrain.SampleHeight(((Component)this).transform.position);
			}
			GameObject val2 = Object.Instantiate<GameObject>(craterPrefab, position, Quaternion.identity);
			ParticleSystem component = val2.GetComponent<ParticleSystem>();
			MainModule main;
			if ((Object)(object)component != (Object)null)
			{
				main = component.main;
				Object.Destroy((Object)(object)val2, ((MainModule)(ref main)).duration);
			}
			else
			{
				ParticleSystem component2 = ((Component)val2.transform.GetChild(0)).GetComponent<ParticleSystem>();
				main = component2.main;
				Object.Destroy((Object)(object)val2, ((MainModule)(ref main)).duration);
			}
		}
		stepPosition = ((Component)this).transform.position;
	}
}
