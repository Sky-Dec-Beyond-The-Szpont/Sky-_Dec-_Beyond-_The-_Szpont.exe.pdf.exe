using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashDuration = 0.15f;
    [SerializeField] private float _dashCooldown = 0.4f;




    private Vector2 _movement;
    private Vector2 _facingDirection = Vector2.down; // domyœlny kierunek
    private Rigidbody2D _rb;

    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    [SerializeField] private float attackRange = 1f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask treasureLayer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
            return;



        // 1. Pobranie ruchu
        _movement = InputManager.Movement;

        // 2. Zapamiêtanie kierunku, jeœli siê rusza
        if (_movement.sqrMagnitude > 0.1f)
        {
            _facingDirection = _movement.normalized;
        }

        HandleDash();
        HandleAttack();
        HandleInteraction();
    }

    private void FixedUpdate()
    {
        if (!_isDashing)
        {
            _rb.linearVelocity = _movement * _moveSpeed;
        }
    }

    // --------------- DASH ---------------
    private void HandleDash()
    {
        if (_dashCooldownTimer > 0)
            _dashCooldownTimer -= Time.deltaTime;

        if (InputManager.DashPressed && !_isDashing && _dashCooldownTimer <= 0)
        {
            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;

            _dashDirection = (_movement.sqrMagnitude > 0.1f)
                ? _movement.normalized        // dash w stronê ruchu
                : _facingDirection;           // dash w stronê patrzenia
        }

        if (_isDashing)
        {
            _rb.linearVelocity = Vector2.Lerp(
                _rb.linearVelocity,
                _dashDirection * _dashSpeed,
                Time.deltaTime * 20f            // g³adkie wejœcie w dash
            );

            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0)
            {
                _isDashing = false;
            }
        }
    }

    // --------------- ATTACK ---------------
    private void HandleAttack()
    {
        if (InputManager.AttackPressed)
        {

            Vector2 attackDir = _facingDirection;

            Debug.Log("ATTACK direction = " + attackDir);

            // RAYCAST W STRONE, W KTORA PATRZY GRACZ
            RaycastHit2D hit = Physics2D.Raycast(transform.position, attackDir, attackRange, enemyLayer);

            if (hit.collider != null)
            {

                Debug.Log("atak");
                EnemyHealth hp = hit.collider.GetComponent<EnemyHealth>();
                if (hp != null)
                {
                    Debug.Log("boban");
                    hp.TakeDamage(attackDamage);
                }
            }
        }
    }

    //private void HandleInteraction()
    //{
    //    if (InputManager.InteractionPressed)
    //    {

    //        Vector2 faceDir = _facingDirection;

    //        Debug.Log("int direction = " + faceDir);

    //        // RAYCAST W STRONE, W KTORA PATRZY GRACZ
    //        RaycastHit2D hit = Physics2D.Raycast(transform.position, faceDir, attackRange, treasureLayer);

    //        if (hit.collider != null)
    //        {

    //            Debug.Log("interakt");
    //            Interactable hp = hit.collider.GetComponent<Interactable>();
    //            //SceneManager.UnloadSceneAsync("SampleScene");
    //            //SceneManager.UnloadSceneAsync("SampleScene");
    //            //SceneManager.SetActiveScene(SceneManager.GetSceneByName("MapScene"));

    //            chosenTower.ReturnFromTower();


    //            //TowerClick.isTowerSceneLoaded = false; // func to go back from this scene to szpont os

    //            //ContentRoomGenerator.ClearRoom();
    //        }
    //    }
    //}

    private void HandleInteraction()
    {
        if (InputManager.InteractionPressed)
        {
            Vector2 faceDir = _facingDirection;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                faceDir,
                attackRange,
                treasureLayer
            );

            if (hit.collider != null)
            {

                Debug.Log("Interakcja ? pokaz UI losowania");

                GambaUI.Instance.Show();
            }
        }
    }

}



