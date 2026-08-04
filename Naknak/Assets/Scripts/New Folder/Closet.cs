using UnityEngine;

public class Closet : MonoBehaviour
{
	private bool hide;

	private SpriteRenderer spriteRenderer;

	public bool Hide => hide;

	private void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void Update()
	{
		Color color = spriteRenderer.color;
		if (Input.GetKeyDown(KeyCode.Z))
		{
			hide = !hide;
			if (hide)
			{
				color.a = 0f;
			}
			if (!hide)
			{
				color.a = 1f;
			}
		}
		spriteRenderer.color = color;
	}
}
