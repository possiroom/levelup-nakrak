using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance { get; private set; }
    [SerializeField] private PlayerMoveController3 playerCtrl;
    [SerializeField] private int health = 100;
    [SerializeField] private Slider healthSlider;

    public bool IsInvincible { get; private set; } = false;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (healthSlider != null) healthSlider.value = health;
    }

    public int GetHealth()
    {
        return health;
    }

    public void AddHealth(int delta)
    {
        if (delta < 0)
        {
            if (IsInvincible) return;
            health = Mathf.Max(0, health + delta);
            StartCoroutine(InvicibleTime(1f));
        }
        else if (delta > 0)
        {
            health = Mathf.Min(100, health + delta);
        } 
        healthSlider.value = health;
    }

    public bool GetIsJumping()
    {
        return playerCtrl.GetIsJumping();
    }

    public bool GetIsMoving()
    {
        return playerCtrl.isMoving;
    }

    public Transform GetPlayerTransform()
    {
        return transform;
    }

    public void TryDamageStone(float obsYPos)
    {
        if (IsInvincible || playerCtrl.GetIsJumping()) return;
        if (Mathf.Abs(obsYPos - (transform.position.y - 0.5f)) <= 1f)
        {
            AddHealth(-10);
            Debug.Log("[PlayerStatus] Damage Stone");
            StartCoroutine(InvicibleTime(1f));
        }
    }

    private IEnumerator InvicibleTime(float time)
    {
        IsInvincible = true;
        yield return new WaitForSeconds(time);
        IsInvincible = false;
    }
}
