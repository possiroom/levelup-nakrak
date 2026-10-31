using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Character_Sprite_Change : MonoBehaviour
{
    [SerializeField] private bool isman;
    [SerializeField] private RuntimeAnimatorController manAnimater;
    [SerializeField] private RuntimeAnimatorController girlAnimater;
    [SerializeField] GameObject sprite;
    Animator animator;

    private void Awake()
    {
        sprite = GameObject.Find("Player");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

        animator = sprite.GetComponent<Animator>();
        if (isman)
            animator.runtimeAnimatorController = manAnimater;
        else
            animator.runtimeAnimatorController = girlAnimater;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
