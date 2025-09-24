using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController2D controller;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprtRendr;
    [SerializeField] private Transform respawn;
    [SerializeField] private Score score;
    [SerializeField] private float speed = 40f;
    [SerializeField] private int maxHp = 10;
    [SerializeField] private GameObject objectToSpawn;
    public float move = 0f;
    private bool jump = false;
    private int currentHp = 0;
    private bool isPaused = false;

    private void OnEnable()
    {
        Actions.DamagePlayer += SufferDamage;
        Actions.RespawnPlayer += RespawnPlayer;
        Actions.HealPlayer += HealPlayer;
        Actions.PauseGame += PauseGame;

        currentHp = maxHp;
        //Actions.SetUpHealth(maxHp);
    }

    private void OnDisable()
    {
        Actions.DamagePlayer -= SufferDamage;
        Actions.RespawnPlayer -= RespawnPlayer;
        Actions.HealPlayer -= HealPlayer;
        Actions.PauseGame -= PauseGame;
    }

    void Update()
    {
        if (currentHp > 0 && !isPaused)
        {
            HandleMovement();
            HandleJump();
            HandleAttack();
        }
    }

    void FixedUpdate()
    {
        HandleMovementAnimation();

        controller.Move(move * Time.fixedDeltaTime, false, jump);
        jump = false;
    }

    private void HandleMovement()
    {
        move = Input.GetAxisRaw("Horizontal") * speed;
    }

    private void HandleJump()
    {
        if (controller.GetGrounded() && Input.GetButtonDown("Jump"))
        {
            jump = true;
            //score.AlterarPotuacao();
            //Actions.ScorePoint();
        }

        Vector2 velocity = controller.GetComponent<Rigidbody2D>().velocity;

        if (velocity.y > 0 && Input.GetButtonUp("Jump"))
        {
            controller.GetComponent<Rigidbody2D>().velocity = new Vector2(velocity.x, velocity.y / 2);
        }
    }

    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1"))
            /*animator.SetTrigger("Attack");*/
            Instantiate(objectToSpawn, transform.position, Quaternion.identity);
    }

    private void HandleMovementAnimation()
    {
        if (move != 0f)
            animator.SetBool("IsIdle", false);
        else
            animator.SetBool("IsIdle", true);
    }

    private void SufferDamage(int damage)
    {
        currentHp -= damage;

        Actions.OnHealthChange(currentHp);

        if (currentHp <= 0)
        {
            Actions.OnPlayerDeath();
            animator.SetTrigger("Death");
        }
    }

    private void RespawnPlayer()
    {
        currentHp = maxHp;
        transform.position = respawn.position;
        Actions.OnHealthChange(currentHp);
    }

    private void HealPlayer(int heal)
    {
        currentHp += heal;

        if (currentHp > maxHp) currentHp = maxHp;

        Actions.OnHealthChange(currentHp);
    }

    private void PauseGame()
    {
        if (isPaused)
            isPaused = false;
        else
            isPaused = true;
    }

    private void DestroySelf()
    {
        Destroy(this.gameObject);
    }
}
