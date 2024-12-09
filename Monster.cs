using System.Collections;
using System.ComponentModel;
using UnityEngine;

public class Monster : MonoBehaviour
{
    public Transform player;            // �÷��̾� ��ġ
    public float moveDistance = 3f;     // ���� �̵� �Ÿ�
    public float moveSpeed = 1f;        // ���� �⺻ �̵� �ӵ�
    public float approachSpeed = 2f;    // �÷��̾� ���� �� �ӵ�
    public float approachRange = 15f;    // ���� ���� �Ÿ�
    private float fixedY;
    private Animator animator;

    private Vector3 startPosition;      // ���� ��ġ
    private bool movingLeft = true;     // �̵� ���� üũ
    private bool isApproaching = false; // �÷��̾� ���� �� ����
    private SpriteRenderer spriteRenderer; // ��������Ʈ ������
    private FadeManager fadeManager;    // ���̵� �Ŵ���
    public Sprite hitSprite;
    private Sprite originalSprite;      // ���� ��������Ʈ ����
    private Coroutine currentAnimationCoroutine;

    private int health = 10;             // ���� ü��
    private bool isHit = false;         // �ǰ� ���� üũ
    private bool isDie = false;         // �׾����� üũ
    private string currentAnimState;     // ���� �ִϸ��̼� ���� Ȯ���� ���� ����

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;  // ���� ��������Ʈ ����
        fadeManager = FindObjectOfType<FadeManager>(); // ���̵� �Ŵ��� ã��
        animator = GetComponent<Animator>();
        //fixedY = transform.position.y;
        currentAnimationCoroutine = StartCoroutine(MovePattern()); // �̵� ���� ����
        currentAnimState = ""; // �ʱ� �ִϸ��̼� ���� �ʱ�ȭ
    }

    void Update()
    {
        if (isHit || isApproaching || isDie) return;

        // �÷��̾���� �Ÿ� üũ
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (isApproaching)
        {
            UpdateFacingDirection(true);
        }
        // ���� ���� ���� �÷��̾ �ְ� ���� ���� �ƴ� ��쿡�� ���� ����
        if (distanceToPlayer <= approachRange && !isApproaching)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }

            StartCoroutine(ApproachPlayer());
        }
        else
        {
            if (!isApproaching && currentAnimationCoroutine == null)
            {
                currentAnimationCoroutine = StartCoroutine(MovePattern());
            }
        }
    }

    private void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            if (isHit)
            {
                StopCoroutine(HandleHit());
                spriteRenderer.sprite = originalSprite;
                isHit = false;
                animator.enabled = true;
            }

            if (!isDie)
                Die();
            return;
        }

        // ���� ���°� �̹� Ȱ��ȭ�� ��쿡�� ����
        if (!isHit)
        {
            isHit = true;
            animator.enabled = false;
            spriteRenderer.sprite = hitSprite;
        }

        // �ڷ�ƾ ����
        StopAllCoroutines();
        StartCoroutine(HandleHit());
    }




    IEnumerator HandleHit()
    {
        yield return new WaitForSeconds(2f); // �ǰ� �� ��� �ð�

        // �ǰ� ���� ����
        spriteRenderer.sprite = originalSprite;
        isHit = false;
        animator.enabled = true;

        // ���¿� ���� �ൿ �簳
        if (!isDie)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= approachRange)
            {
                StartCoroutine(ApproachPlayer()); // �÷��̾� �߰� �簳
            }
            else
            {
                currentAnimationCoroutine = StartCoroutine(MovePattern()); // �̵� ���� �簳
            }
        }
    }


    // �̵� ����
    IEnumerator MovePattern()
    {
       
        while (!isApproaching)
        {
            UpdateFacingDirection(false);
            // ���� ������ �������� �̵�
            float targetX = movingLeft ? startPosition.x - 5f : startPosition.x + 5f;
           
            Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

            // �̵��� �����ϸ� �������� �̵�
            float elapsedTime = 0f;
            while (elapsedTime < 3f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // ������ ��ȯ
            movingLeft = !movingLeft;
            spriteRenderer.flipX = movingLeft;

            // 2�� ��� (Idle �ִϸ��̼�)
            animator.SetBool("IsJump", false);
            animator.SetBool("IsIdle", true);
            yield return new WaitForSeconds(2f);

            animator.SetBool("IsJump", true);
            animator.SetBool("IsIdle", false);
        }
    }

    IEnumerator ApproachPlayer()
    {
        isApproaching = true;
        animator.SetBool("IsJump", true);
        animator.SetBool("IsIdle", false);

        while (!isDie && Vector3.Distance(transform.position, player.position) > 0.1f)
        {
            if (Vector3.Distance(transform.position, player.position) > approachRange)
            {
                isApproaching = false;
                currentAnimationCoroutine = StartCoroutine(MovePattern());
                yield break;
            }

            UpdateFacingDirection(true);

            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, approachSpeed * Time.deltaTime);

            yield return null;
        }

        if (!isDie)
        {
            isApproaching = false;
            currentAnimationCoroutine = StartCoroutine(MovePattern());
        }
       
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Trap"))
        {
            Die();
        }
    }
    private void UpdateFacingDirection(bool usePlayerDirection = true)
    {
        if (usePlayerDirection)
        {
            // �÷��̾� ������ �������� ���� ����
            if (player.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true; // �÷��̾ ���ʿ� ������ ������ �ٶ�
            }
            else
            {
                spriteRenderer.flipX = false; // �÷��̾ �����ʿ� ������ �������� �ٶ�
            }
        }
        else
        {
            // �̵� ����(movingLeft)�� �������� ���� ����
            spriteRenderer.flipX = movingLeft;
        }
    }



    private void Die()
    {
        if (isDie) return;

        isDie = true;
        animator.SetBool("IsDie", true);
        StopCoroutine(ApproachPlayer());
        StopAllCoroutines();
        isApproaching = false;
        isHit = false;

        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        AudioManager.Instance.PlaySFX(SFX.EnemyDie1);

        Destroy(gameObject, 0.8f);
    }

    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                TakeDamage(1);
            }
        }
    }
}