
using System.Collections.Generic;
using UnityEngine;

public class Character_Sprite_Change : MonoBehaviour
{
    [SerializeField] private bool isman;
    [SerializeField] private RuntimeAnimatorController manAnimater;
    [SerializeField] private RuntimeAnimatorController girlAnimater;
    [SerializeField] GameObject sprite;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Dictionary<Sprite, Sprite> spriteCache = new Dictionary<Sprite, Sprite>();

    private void Awake()
    {
        sprite = GameObject.Find("Player");

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


        animator = sprite.GetComponent<Animator>();
        spriteRenderer = sprite.GetComponent<SpriteRenderer>();

        if (isman)
            animator.runtimeAnimatorController = manAnimater;
        else
            animator.runtimeAnimatorController = girlAnimater;

    }

   
}
